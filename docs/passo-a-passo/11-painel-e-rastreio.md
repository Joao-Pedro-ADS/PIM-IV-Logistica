# 11 — Painel inicial e rastreio da caixa (≈ 25 min)

**Objetivo:** a página inicial mostra os números do dia, e a tela **Rastrear caixa** (**RF19**) mostra todo o caminho de uma caixa. Essa tela resolve o **problema central** do contexto, a falta de rastreabilidade.

## 1. Painel

Substitua `src/Pim.Web/Controllers/HomeController.cs`:

**`src/Pim.Web/Controllers/HomeController.cs`**

```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;
using Pim.Web.Models;

namespace Pim.Web.Controllers;

public class HomeController : Controller
{
    private readonly PimDbContext _db;

    public HomeController(PimDbContext db) => _db = db;

    // Painel com os números do dia
    public async Task<IActionResult> Index()
    {
        var limiteCnh = DateTime.Today.AddDays(30);

        ViewBag.PedidosAbertos = await _db.PedidosVenda.CountAsync(p => p.Status == StatusPedido.Aberto);
        ViewBag.CarregamentosEmRota = await _db.Carregamentos.CountAsync(c => c.Status == StatusCarregamento.EmRota);
        ViewBag.VeiculosDisponiveis = await _db.Veiculos.CountAsync(v => v.Situacao == SituacaoVeiculo.Disponivel);
        ViewBag.RecebimentosPendentes = await _db.Recebimentos.CountAsync(r => r.Status == StatusRecebimento.AguardandoAprovacao);
        ViewBag.CnhVencendo = await _db.Motoristas.CountAsync(m => m.ValidadeCnh <= limiteCnh);

        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
```

Substitua `src/Pim.Web/Views/Home/Index.cshtml` e **apague** `Views/Home/Privacy.cshtml`:

**`src/Pim.Web/Views/Home/Index.cshtml`**

```cshtml
@{
    ViewData["Title"] = "Início";
}

<h1>Olá, @User.Identity?.Name</h1>

<table>
    <tbody>
        <tr><th>Pedidos abertos</th><td>@ViewBag.PedidosAbertos</td></tr>
        <tr><th>Carregamentos em rota</th><td>@ViewBag.CarregamentosEmRota</td></tr>
        <tr><th>Veículos disponíveis</th><td>@ViewBag.VeiculosDisponiveis</td></tr>
        <tr><th>Recebimentos aguardando aprovação</th><td>@ViewBag.RecebimentosPendentes</td></tr>
        <tr><th>Motoristas com CNH vencida ou vencendo em 30 dias</th><td>@ViewBag.CnhVencendo</td></tr>
    </tbody>
</table>
```

## 2. Rastrear caixa

Crie `src/Pim.Web/Controllers/CaixasController.cs`:

**`src/Pim.Web/Controllers/CaixasController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente")]
public class CaixasController : Controller
{
    private readonly PimDbContext _db;

    public CaixasController(PimDbContext db) => _db = db;

    // RF19: histórico de uma caixa pelo código
    public async Task<IActionResult> Index(string? codigo)
    {
        ViewBag.Codigo = codigo;
        if (string.IsNullOrWhiteSpace(codigo))
            return View();

        var codigoBusca = codigo.Trim().ToUpper();
        var caixa = await _db.Caixas
            .Include(c => c.Produto)
            .Include(c => c.Recebimento).ThenInclude(r => r!.NotaFiscal).ThenInclude(n => n!.Fornecedor)
            .Include(c => c.PedidoVenda).ThenInclude(p => p!.Cliente)
            .Include(c => c.Movimentacoes).ThenInclude(m => m.Usuario)
            .FirstOrDefaultAsync(c => c.Codigo == codigoBusca);

        if (caixa == null)
            ViewBag.Erro = "Caixa não encontrada.";

        return View(caixa);
    }
}
```

Crie `src/Pim.Web/Views/Caixas/Index.cshtml`:

**`src/Pim.Web/Views/Caixas/Index.cshtml`**

```cshtml
@model Caixa?
@{
    ViewData["Title"] = "Rastrear caixa";
}

<h1>Rastrear caixa</h1>

<form asp-action="Index" method="get">
    <label for="codigo">Código da caixa</label>
    <input id="codigo" name="codigo" value="@ViewBag.Codigo" placeholder="CX00000001" required />
    <button type="submit">Buscar</button>
</form>

@if (ViewBag.Erro != null)
{
    <p>@ViewBag.Erro</p>
}

@if (Model != null)
{
    <h2>Caixa @Model.Codigo</h2>
    <p>Produto: @(Model.Produto?.Descricao)</p>
    <p>Entrou pela nota @(Model.Recebimento?.NotaFiscal?.Numero) de @(Model.Recebimento?.NotaFiscal?.Fornecedor?.Nome)</p>
    <p>Pedido: @(Model.PedidoVenda != null ? $"{Model.PedidoVenda.Numero} - {Model.PedidoVenda.Cliente?.Nome}" : "nenhum")</p>
    <p>Status atual: @Model.Status</p>

    <h2>Histórico</h2>
    <table>
        <thead>
            <tr><th>Data e hora</th><th>Movimentação</th><th>Usuário</th><th>Carregamento</th></tr>
        </thead>
        <tbody>
            @foreach (var m in Model.Movimentacoes.OrderBy(m => m.DataHora))
            {
                <tr>
                    <td>@m.DataHora.ToString("dd/MM/yyyy HH:mm")</td>
                    <td>@m.Tipo</td>
                    <td>@(m.Usuario?.Nome)</td>
                    <td>@(m.CarregamentoId?.ToString() ?? "-")</td>
                </tr>
            }
        </tbody>
    </table>
}
```

> Por enquanto o histórico só tem a **Entrada**. Quando o app bipar carregamento, saída, entrega e retorno, cada bip grava uma `MovimentacaoCaixa` e ela aparece aqui automaticamente.

## Testar (como `gerente`)
- [ ] o início mostra 5 pedidos abertos, 1 recebimento aguardando etc.
- [ ] buscar `cx00000001` (minúsculo funciona) mostra o Produto A, a nota 4521 e a entrada feita pela Juliana
- [ ] buscar `XX` mostra "Caixa não encontrada."
