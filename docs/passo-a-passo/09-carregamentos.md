# 09 — Carregamentos (≈ 40 min)

**Objetivo (RF10):** o programador escolhe veículo, motorista, data e pedidos. É o passo com **mais regras**, e todas ficam no serviço:

| Regra | O que o serviço confere |
|---|---|
| RN06 | O pedido precisa estar `Aberto`, ou seja, fora de outro carregamento |
| RN08 | Pelo menos um pedido |
| RN09 | A soma das caixas não pode passar do limite do veículo |
| RN17 | O veículo não pode estar em manutenção |
| RN18 | A CNH do motorista precisa estar válida na data |
| RN19 | Veículo e motorista não podem estar em outro carregamento em aberto |

## 1. Serviço

Crie `src/Pim.Core/Servicos/CarregamentoService.cs`:

**`src/Pim.Core/Servicos/CarregamentoService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

public class CarregamentoService
{
    private readonly PimDbContext _db;

    public CarregamentoService(PimDbContext db) => _db = db;

    // RF10: criar carregamento aplicando as regras
    public async Task<Carregamento> CriarAsync(int veiculoId, int motoristaId, DateTime data, List<int> pedidoIds)
    {
        if (pedidoIds.Count == 0)
            throw new RegraNegocioException("RN08: escolha pelo menos um pedido.");

        var veiculo = await _db.Veiculos.FindAsync(veiculoId)
            ?? throw new RegraNegocioException("Veículo não encontrado.");
        if (veiculo.Situacao == SituacaoVeiculo.EmManutencao)
            throw new RegraNegocioException("RN17: veículo em manutenção não pode ser usado em carregamento.");

        var motorista = await _db.Motoristas.FindAsync(motoristaId)
            ?? throw new RegraNegocioException("Motorista não encontrado.");
        if (!motorista.CnhValidaEm(data))
            throw new RegraNegocioException("RN18: a CNH do motorista estará vencida na data do carregamento.");

        // RN19: um carregamento por vez (em aberto = planejado ou em rota)
        var emAberto = new[] { StatusCarregamento.Planejado, StatusCarregamento.EmRota };
        if (await _db.Carregamentos.AnyAsync(c => c.VeiculoId == veiculoId && emAberto.Contains(c.Status)))
            throw new RegraNegocioException("RN19: este veículo já está em outro carregamento.");
        if (await _db.Carregamentos.AnyAsync(c => c.MotoristaId == motoristaId && emAberto.Contains(c.Status)))
            throw new RegraNegocioException("RN19: este motorista já está em outro carregamento.");

        var pedidos = await _db.PedidosVenda.Include(p => p.Itens)
            .Where(p => pedidoIds.Contains(p.Id))
            .ToListAsync();

        // RN06: o pedido vai inteiro em um único carregamento
        if (pedidos.Any(p => p.Status != StatusPedido.Aberto))
            throw new RegraNegocioException("RN06: um dos pedidos não está aberto ou já está em outro carregamento.");

        // RN09: limite de caixas do veículo
        int totalCaixas = pedidos.Sum(p => p.TotalCaixas);
        if (totalCaixas > veiculo.LimiteCaixas)
            throw new RegraNegocioException($"RN09: são {totalCaixas} caixas e o veículo aceita no máximo {veiculo.LimiteCaixas}.");

        var carregamento = new Carregamento
        {
            Veiculo = veiculo,
            Motorista = motorista,
            DataPrevista = data.Date,
            Status = StatusCarregamento.Planejado
        };
        foreach (var pedido in pedidos)
        {
            pedido.Status = StatusPedido.EmCarregamento;
            carregamento.Pedidos.Add(new CarregamentoPedido { PedidoVenda = pedido });
        }

        _db.Carregamentos.Add(carregamento);
        await _db.SaveChangesAsync();
        return carregamento;
    }
}
```

E registre no `Program.cs`:

```csharp
builder.Services.AddScoped<CarregamentoService>();
```

## 2. Controller

Crie `src/Pim.Web/Controllers/CarregamentosController.cs`:

**`src/Pim.Web/Controllers/CarregamentosController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;
using Pim.Core.Servicos;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente,Programador")]
public class CarregamentosController : Controller
{
    private readonly PimDbContext _db;
    private readonly CarregamentoService _carregamentoService;

    public CarregamentosController(PimDbContext db, CarregamentoService carregamentoService)
    {
        _db = db;
        _carregamentoService = carregamentoService;
    }

    public async Task<IActionResult> Index()
    {
        var carregamentos = await _db.Carregamentos
            .Include(c => c.Veiculo)
            .Include(c => c.Motorista)
            .Include(c => c.Caixas)
            .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Itens)
            .OrderByDescending(c => c.DataPrevista)
            .ToListAsync();
        return View(carregamentos);
    }

    // RF10
    public async Task<IActionResult> Criar()
    {
        await CarregarListas();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Criar(int veiculoId, int motoristaId, DateTime dataPrevista, List<int> pedidoIds)
    {
        try
        {
            var carregamento = await _carregamentoService.CriarAsync(veiculoId, motoristaId, dataPrevista, pedidoIds);
            TempData["Mensagem"] = "Carregamento criado.";
            return RedirectToAction(nameof(Detalhes), new { id = carregamento.Id });
        }
        catch (RegraNegocioException ex)
        {
            ViewBag.Erro = ex.Message;
            await CarregarListas();
            return View();
        }
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var carregamento = await _db.Carregamentos
            .Include(c => c.Veiculo)
            .Include(c => c.Motorista)
            .Include(c => c.Caixas).ThenInclude(cx => cx.Produto)
            .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Cliente)
            .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Itens)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (carregamento == null) return NotFound();
        return View(carregamento);
    }

    // mostra todos os veículos e motoristas: quem não pode ser usado é barrado pelo serviço (RN17, RN18, RN19)
    private async Task CarregarListas()
    {
        ViewBag.Veiculos = await _db.Veiculos.OrderBy(v => v.Placa).ToListAsync();
        ViewBag.Motoristas = await _db.Motoristas.OrderBy(m => m.Nome).ToListAsync();
        ViewBag.Pedidos = await _db.PedidosVenda
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
            .Where(p => p.Status == StatusPedido.Aberto)
            .OrderBy(p => p.DataEmissao)
            .ToListAsync();
    }
}
```

> A tela **mostra todos** os veículos e motoristas, inclusive os que não podem ser usados. É de propósito: assim dá para ver as regras funcionando, porque o serviço barra com a mensagem da RN.

## 3. Telas

Crie a pasta `src/Pim.Web/Views/Carregamentos/`:

**`src/Pim.Web/Views/Carregamentos/Index.cshtml`**

```cshtml
@model List<Carregamento>
@{
    ViewData["Title"] = "Carregamentos";
}

<h1>Carregamentos</h1>

<p><a asp-action="Criar">Novo carregamento</a></p>

<table>
    <thead>
        <tr>
            <th>Nº</th>
            <th>Data</th>
            <th>Veículo</th>
            <th>Motorista</th>
            <th>Pedidos</th>
            <th>Caixas bipadas</th>
            <th>Status</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var c in Model)
        {
            var totalCaixas = c.Pedidos.Sum(cp => cp.PedidoVenda?.TotalCaixas ?? 0);
            <tr>
                <td>@c.Id</td>
                <td>@c.DataPrevista.ToString("dd/MM/yyyy")</td>
                <td>@(c.Veiculo?.Placa)</td>
                <td>@(c.Motorista?.Nome)</td>
                <td>@c.Pedidos.Count</td>
                <td>@c.Caixas.Count de @totalCaixas</td>
                <td>@c.Status</td>
                <td><a asp-action="Detalhes" asp-route-id="@c.Id">Detalhes</a></td>
            </tr>
        }
    </tbody>
</table>

@if (!Model.Any())
{
    <p>Nenhum carregamento cadastrado.</p>
}
```

**`src/Pim.Web/Views/Carregamentos/Criar.cshtml`**

```cshtml
@{
    ViewData["Title"] = "Novo carregamento";
    var veiculos = (List<Veiculo>)ViewBag.Veiculos;
    var motoristas = (List<Motorista>)ViewBag.Motoristas;
    var pedidos = (List<PedidoVenda>)ViewBag.Pedidos;
}

<h1>Novo carregamento</h1>

@if (ViewBag.Erro != null)
{
    <p>@ViewBag.Erro</p>
}

<form asp-action="Criar" method="post">
    <p>
        <label for="veiculoId">Veículo</label><br />
        <select id="veiculoId" name="veiculoId">
            @foreach (var v in veiculos)
            {
                <option value="@v.Id">@v.Placa - @v.Modelo (até @v.LimiteCaixas caixas) - @v.Situacao</option>
            }
        </select>
    </p>

    <p>
        <label for="motoristaId">Motorista</label><br />
        <select id="motoristaId" name="motoristaId">
            @foreach (var m in motoristas)
            {
                <option value="@m.Id">@m.Nome (CNH válida até @m.ValidadeCnh.ToString("dd/MM/yyyy"))</option>
            }
        </select>
    </p>

    <p>
        <label for="dataPrevista">Data</label><br />
        <input type="date" id="dataPrevista" name="dataPrevista" value="@DateTime.Today.ToString("yyyy-MM-dd")" required />
    </p>

    <fieldset>
        <legend>Pedidos abertos</legend>
        @if (!pedidos.Any())
        {
            <p>Nenhum pedido aberto.</p>
        }
        @foreach (var p in pedidos)
        {
            <p>
                <label>
                    <input type="checkbox" name="pedidoIds" value="@p.Id" />
                    Pedido @p.Numero - @(p.Cliente?.Nome) - @p.TotalCaixas caixas
                </label>
            </p>
        }
    </fieldset>

    <button type="submit">Criar carregamento</button>
    <a asp-action="Index">Voltar</a>
</form>
```

**`src/Pim.Web/Views/Carregamentos/Detalhes.cshtml`**

```cshtml
@model Carregamento
@{
    ViewData["Title"] = $"Carregamento {Model.Id}";
    var totalCaixas = Model.Pedidos.Sum(cp => cp.PedidoVenda?.TotalCaixas ?? 0);
}

<h1>Carregamento @Model.Id</h1>

<p>Data: @Model.DataPrevista.ToString("dd/MM/yyyy")</p>
<p>Veículo: @(Model.Veiculo?.Placa) - @(Model.Veiculo?.Modelo)</p>
<p>Motorista: @(Model.Motorista?.Nome)</p>
<p>Status: @Model.Status</p>
<p>Saída: @(Model.DataSaida?.ToString("dd/MM/yyyy HH:mm") ?? "-")</p>
<p>Volta: @(Model.DataVolta?.ToString("dd/MM/yyyy HH:mm") ?? "-")</p>
<p>Caixas bipadas: @Model.Caixas.Count de @totalCaixas @(Model.Caixas.Count == totalCaixas ? "(completo)" : "(incompleto)")</p>

<h2>Pedidos</h2>
<table>
    <thead>
        <tr><th>Pedido</th><th>Cliente</th><th>Caixas</th><th>Entrega</th><th>Motivo</th></tr>
    </thead>
    <tbody>
        @foreach (var cp in Model.Pedidos)
        {
            <tr>
                <td><a asp-controller="Pedidos" asp-action="Detalhes" asp-route-id="@cp.PedidoVendaId">@(cp.PedidoVenda?.Numero)</a></td>
                <td>@(cp.PedidoVenda?.Cliente?.Nome)</td>
                <td>@(cp.PedidoVenda?.TotalCaixas)</td>
                <td>@cp.Resultado</td>
                <td>@cp.Motivo</td>
            </tr>
        }
    </tbody>
</table>

<h2>Caixas no veículo</h2>
@if (!Model.Caixas.Any())
{
    <p>Nenhuma caixa bipada ainda. A bipagem é feita pelo aplicativo.</p>
}
else
{
    <table>
        <thead>
            <tr><th>Código</th><th>Produto</th><th>Status</th></tr>
        </thead>
        <tbody>
            @foreach (var caixa in Model.Caixas)
            {
                <tr>
                    <td>@caixa.Codigo</td>
                    <td>@(caixa.Produto?.Descricao)</td>
                    <td>@caixa.Status</td>
                </tr>
            }
        </tbody>
    </table>
}

<p><a asp-action="Index">Voltar</a></p>
```

## Testar (como `programador`) — cada linha deve dar a mensagem indicada

| Veículo | Motorista | Pedidos | Resultado esperado |
|---|---|---|---|
| GHI7J89 (Truck) | Marcos | 5001 | RN17 |
| ABC1D23 (Van) | Pedro Costa | 5001 | RN18 |
| ABC1D23 (Van, 15) | Marcos | 5001 + 5003 (18 caixas) | RN09 |
| ABC1D23 | Marcos | nenhum | RN08 |
| ABC1D23 | Marcos | 5001 | **Carregamento criado** |
| ABC1D23 | João | 5002 | RN19 (veículo) |
| DEF4G56 | Marcos | 5002 | RN19 (motorista) |

Também confira:
- [ ] o pedido 5001 aparece como `EmCarregamento` na tela de pedidos
- [ ] cancelar o pedido 5001 (como gerente) deixa o carregamento como `Cancelado` e libera a van
