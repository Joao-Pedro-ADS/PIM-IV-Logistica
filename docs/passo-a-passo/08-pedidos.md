# 08 — Pedidos de venda (≈ 30 min)

**Objetivo:** consultar os pedidos e o status de cada um (**RF08**) e cancelar (**RF09**), respeitando a **RN07** (só antes de o veículo sair).

Aqui aparece o primeiro **serviço**: a regra fica no `Pim.Core`, não no controller. Assim a API vai poder usar a mesma regra depois.

## 1. Exceção de regra de negócio

Crie a pasta `src/Pim.Core/Servicos/` e o arquivo:

**`src/Pim.Core/Servicos/RegraNegocioException.cs`**

```csharp
namespace Pim.Core.Servicos;

// Lançada quando uma regra de negócio (RN) é violada
public class RegraNegocioException : Exception
{
    public RegraNegocioException(string mensagem) : base(mensagem) { }
}
```

## 2. Serviço de pedidos

**`src/Pim.Core/Servicos/PedidoService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

public class PedidoService
{
    private readonly PimDbContext _db;

    public PedidoService(PimDbContext db) => _db = db;

    // RF09 / RN07: só cancela antes de o veículo sair
    public async Task CancelarAsync(int pedidoId)
    {
        var pedido = await _db.PedidosVenda.FindAsync(pedidoId)
            ?? throw new RegraNegocioException("Pedido não encontrado.");

        if (pedido.Status != StatusPedido.Aberto && pedido.Status != StatusPedido.EmCarregamento)
            throw new RegraNegocioException("RN07: o pedido só pode ser cancelado antes de o veículo sair.");

        // tira o pedido do carregamento; se o carregamento ficar vazio, ele é cancelado
        var vinculos = await _db.CarregamentoPedidos
            .Include(cp => cp.Carregamento).ThenInclude(c => c!.Pedidos)
            .Where(cp => cp.PedidoVendaId == pedidoId && cp.Carregamento!.Status == StatusCarregamento.Planejado)
            .ToListAsync();
        foreach (var vinculo in vinculos)
        {
            if (vinculo.Carregamento!.Pedidos.Count == 1)
                vinculo.Carregamento.Status = StatusCarregamento.Cancelado;
            _db.CarregamentoPedidos.Remove(vinculo);
        }

        // as caixas que já tinham sido bipadas voltam a ficar livres no armazém
        var caixas = await _db.Caixas.Where(c => c.PedidoVendaId == pedidoId).ToListAsync();
        foreach (var caixa in caixas)
        {
            caixa.PedidoVendaId = null;
            caixa.CarregamentoId = null;
            caixa.Status = StatusCaixa.NoArmazem;
        }

        pedido.Status = StatusPedido.Cancelado;
        await _db.SaveChangesAsync();
    }
}
```

## 3. Registrar o serviço

No `Program.cs`, adicione `using Pim.Core.Servicos;` no topo e, abaixo do `AddDbContext`:

```csharp
builder.Services.AddScoped<PedidoService>();
```

E no `Views/_ViewImports.cshtml` acrescente a linha:

```cshtml
@using Pim.Core.Servicos
```

## 4. Controller

Crie `src/Pim.Web/Controllers/PedidosController.cs`:

**`src/Pim.Web/Controllers/PedidosController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;
using Pim.Core.Servicos;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente,Comercial,Programador")]
public class PedidosController : Controller
{
    private readonly PimDbContext _db;
    private readonly PedidoService _pedidoService;

    public PedidosController(PimDbContext db, PedidoService pedidoService)
    {
        _db = db;
        _pedidoService = pedidoService;
    }

    // RF08: lista com filtro por status e busca por número/cliente
    public async Task<IActionResult> Index(StatusPedido? status, string? busca)
    {
        var consulta = _db.PedidosVenda
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
            .AsQueryable();

        if (status != null)
            consulta = consulta.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(p => p.Cliente!.Nome.Contains(busca) || p.Numero.ToString() == busca);

        ViewBag.Status = status;
        ViewBag.Busca = busca;
        return View(await consulta.OrderByDescending(p => p.DataEmissao).ToListAsync());
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var pedido = await _db.PedidosVenda
            .Include(p => p.Cliente)
            .Include(p => p.Endereco)
            .Include(p => p.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null) return NotFound();

        // em quais carregamentos o pedido já esteve
        ViewBag.Carregamentos = await _db.CarregamentoPedidos
            .Include(cp => cp.Carregamento).ThenInclude(c => c!.Veiculo)
            .Where(cp => cp.PedidoVendaId == id)
            .ToListAsync();

        return View(pedido);
    }

    // RF09
    [HttpPost]
    [Authorize(Roles = "Gerente")]
    public async Task<IActionResult> Cancelar(int id)
    {
        try
        {
            await _pedidoService.CancelarAsync(id);
            TempData["Mensagem"] = "Pedido cancelado.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }
}
```

## 5. Telas

Crie a pasta `src/Pim.Web/Views/Pedidos/`:

**`src/Pim.Web/Views/Pedidos/Index.cshtml`**

```cshtml
@model List<PedidoVenda>
@{
    ViewData["Title"] = "Pedidos de venda";
}

<h1>Pedidos de venda</h1>

<form asp-action="Index" method="get">
    <select name="status">
        <option value="">Todos os status</option>
        @foreach (var s in Enum.GetValues<StatusPedido>())
        {
            <option value="@s" selected="@(ViewBag.Status == s)">@s</option>
        }
    </select>
    <input name="busca" value="@ViewBag.Busca" placeholder="Número ou cliente" />
    <button type="submit">Filtrar</button>
</form>

<table>
    <thead>
        <tr>
            <th>Número</th>
            <th>Cliente</th>
            <th>Data</th>
            <th>Caixas</th>
            <th>Status</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var p in Model)
        {
            <tr>
                <td>@p.Numero</td>
                <td>@(p.Cliente?.Nome)</td>
                <td>@p.DataEmissao.ToString("dd/MM/yyyy")</td>
                <td>@p.TotalCaixas</td>
                <td>@p.Status</td>
                <td><a asp-action="Detalhes" asp-route-id="@p.Id">Detalhes</a></td>
            </tr>
        }
    </tbody>
</table>

@if (!Model.Any())
{
    <p>Nenhum pedido encontrado.</p>
}
```

**`src/Pim.Web/Views/Pedidos/Detalhes.cshtml`**

```cshtml
@model PedidoVenda
@{
    ViewData["Title"] = $"Pedido {Model.Numero}";
    var carregamentos = (List<CarregamentoPedido>)ViewBag.Carregamentos;
    var podeCancelar = Model.Status == StatusPedido.Aberto || Model.Status == StatusPedido.EmCarregamento;
}

<h1>Pedido @Model.Numero</h1>

<p>Cliente: @(Model.Cliente?.Nome)</p>
<p>Endereço de entrega: @Model.Endereco</p>
<p>Data: @Model.DataEmissao.ToString("dd/MM/yyyy")</p>
<p>Status: @Model.Status</p>

<h2>Itens</h2>
<table>
    <thead>
        <tr><th>Código</th><th>Produto</th><th>Caixas</th></tr>
    </thead>
    <tbody>
        @foreach (var item in Model.Itens)
        {
            <tr>
                <td>@(item.Produto?.Codigo)</td>
                <td>@(item.Produto?.Descricao)</td>
                <td>@item.Quantidade</td>
            </tr>
        }
    </tbody>
</table>
<p>Total: @Model.TotalCaixas caixas</p>

<h2>Carregamentos</h2>
@if (!carregamentos.Any())
{
    <p>O pedido ainda não entrou em carregamento.</p>
}
else
{
    <table>
        <thead>
            <tr><th>Carregamento</th><th>Veículo</th><th>Data</th><th>Entrega</th><th>Motivo</th></tr>
        </thead>
        <tbody>
            @foreach (var cp in carregamentos)
            {
                <tr>
                    <td>@cp.CarregamentoId</td>
                    <td>@(cp.Carregamento?.Veiculo?.Placa)</td>
                    <td>@(cp.Carregamento?.DataPrevista.ToString("dd/MM/yyyy"))</td>
                    <td>@cp.Resultado</td>
                    <td>@cp.Motivo</td>
                </tr>
            }
        </tbody>
    </table>
}

@if (User.IsInRole("Gerente") && podeCancelar)
{
    <form asp-action="Cancelar" asp-route-id="@Model.Id" method="post">
        <button type="submit">Cancelar pedido</button>
    </form>
}

<p><a asp-action="Index">Voltar</a></p>
```

## Testar
- [ ] a lista mostra os 5 pedidos `Aberto`
- [ ] o filtro por status e a busca por "Padaria" ou "5001" funcionam
- [ ] **Detalhes** mostra os itens, o endereço e o total de caixas
- [ ] como `gerente`: **Cancelar pedido** funciona, e cancelar de novo não aparece mais
- [ ] como `comercial`: vê os pedidos, mas sem o botão de cancelar
