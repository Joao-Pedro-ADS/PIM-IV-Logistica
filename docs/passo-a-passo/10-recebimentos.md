# 10 — Recebimentos: conferência e aprovação (≈ 30 min)

**Objetivo (RF07):** a bipagem da entrada é feita no app. No Web, o **gerente** vê a conferência (nota × caixas bipadas) e **aprova** a entrada que tem diferença (**RN03**).

## 1. Serviço

Crie `src/Pim.Core/Servicos/RecebimentoService.cs`:

**`src/Pim.Core/Servicos/RecebimentoService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

// Uma linha da conferência: produto, quanto a nota diz e quanto foi bipado
public record LinhaConferencia(string Produto, int NaNota, int Bipadas)
{
    public int Diferenca => Bipadas - NaNota;
}

public class RecebimentoService
{
    private readonly PimDbContext _db;

    public RecebimentoService(PimDbContext db) => _db = db;

    // RF07: compara o que foi bipado com a nota
    public async Task<List<LinhaConferencia>> ConferirAsync(int recebimentoId)
    {
        var recebimento = await _db.Recebimentos
            .Include(r => r.NotaFiscal).ThenInclude(n => n!.Itens).ThenInclude(i => i.Produto)
            .Include(r => r.Caixas)
            .FirstOrDefaultAsync(r => r.Id == recebimentoId)
            ?? throw new RegraNegocioException("Recebimento não encontrado.");

        return recebimento.NotaFiscal!.Itens
            .Select(item => new LinhaConferencia(
                item.Produto!.Descricao,
                item.Quantidade,
                recebimento.Caixas.Count(c => c.ProdutoId == item.ProdutoId)))
            .ToList();
    }

    // RN03: só o gerente aceita a entrada com diferença
    public async Task AprovarAsync(int recebimentoId, int gerenteId, string? justificativa)
    {
        var recebimento = await _db.Recebimentos.FindAsync(recebimentoId)
            ?? throw new RegraNegocioException("Recebimento não encontrado.");

        if (recebimento.Status != StatusRecebimento.AguardandoAprovacao)
            throw new RegraNegocioException("Este recebimento não está aguardando aprovação.");
        if (string.IsNullOrWhiteSpace(justificativa))
            throw new RegraNegocioException("Informe a justificativa para aceitar a diferença.");

        recebimento.Status = StatusRecebimento.Finalizado;
        recebimento.AprovadoPorId = gerenteId;
        recebimento.Justificativa = justificativa.Trim();
        recebimento.DataFim = DateTime.Now;
        await _db.SaveChangesAsync();
    }
}
```

> `LinhaConferencia` é um `record`: uma classe simples, só para carregar dados até a tela.

Registre no `Program.cs`:

```csharp
builder.Services.AddScoped<RecebimentoService>();
```

## 2. Controller

Crie `src/Pim.Web/Controllers/RecebimentosController.cs`:

**`src/Pim.Web/Controllers/RecebimentosController.cs`**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Servicos;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente")]
public class RecebimentosController : Controller
{
    private readonly PimDbContext _db;
    private readonly RecebimentoService _recebimentoService;

    public RecebimentosController(PimDbContext db, RecebimentoService recebimentoService)
    {
        _db = db;
        _recebimentoService = recebimentoService;
    }

    public async Task<IActionResult> Index()
    {
        var recebimentos = await _db.Recebimentos
            .Include(r => r.NotaFiscal).ThenInclude(n => n!.Fornecedor)
            .Include(r => r.Conferente)
            .Include(r => r.Caixas)
            .OrderByDescending(r => r.DataInicio)
            .ToListAsync();
        return View(recebimentos);
    }

    // RF07: conferência com a nota
    public async Task<IActionResult> Detalhes(int id)
    {
        var recebimento = await _db.Recebimentos
            .Include(r => r.NotaFiscal).ThenInclude(n => n!.Fornecedor)
            .Include(r => r.Conferente)
            .Include(r => r.AprovadoPor)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (recebimento == null) return NotFound();

        ViewBag.Linhas = await _recebimentoService.ConferirAsync(id);
        return View(recebimento);
    }

    // RN03: gerente aceita a entrada com diferença
    [HttpPost]
    public async Task<IActionResult> Aprovar(int id, string? justificativa)
    {
        var gerenteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _recebimentoService.AprovarAsync(id, gerenteId, justificativa);
            TempData["Mensagem"] = "Entrada aprovada.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }
}
```

> O ID do gerente vem do **cookie de login** (`ClaimTypes.NameIdentifier`), não do formulário. Assim ninguém aprova em nome de outra pessoa.

## 3. Telas

Crie a pasta `src/Pim.Web/Views/Recebimentos/`:

**`src/Pim.Web/Views/Recebimentos/Index.cshtml`**

```cshtml
@model List<Recebimento>
@{
    ViewData["Title"] = "Recebimentos";
}

<h1>Recebimentos</h1>

<table>
    <thead>
        <tr>
            <th>Data</th>
            <th>Nota</th>
            <th>Fornecedor</th>
            <th>Conferente</th>
            <th>Caixas bipadas</th>
            <th>Status</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var r in Model)
        {
            <tr>
                <td>@r.DataInicio.ToString("dd/MM/yyyy HH:mm")</td>
                <td>@(r.NotaFiscal?.Numero)</td>
                <td>@(r.NotaFiscal?.Fornecedor?.Nome)</td>
                <td>@(r.Conferente?.Nome)</td>
                <td>@r.Caixas.Count de @(r.NotaFiscal?.QuantidadeVolumes)</td>
                <td>@r.Status</td>
                <td><a asp-action="Detalhes" asp-route-id="@r.Id">Detalhes</a></td>
            </tr>
        }
    </tbody>
</table>
```

**`src/Pim.Web/Views/Recebimentos/Detalhes.cshtml`**

```cshtml
@model Recebimento
@{
    ViewData["Title"] = "Recebimento";
    var linhas = (List<LinhaConferencia>)ViewBag.Linhas;
}

<h1>Recebimento da nota @(Model.NotaFiscal?.Numero)</h1>

<p>Fornecedor: @(Model.NotaFiscal?.Fornecedor?.Nome)</p>
<p>Conferente: @(Model.Conferente?.Nome)</p>
<p>Início: @Model.DataInicio.ToString("dd/MM/yyyy HH:mm")</p>
<p>Fim: @(Model.DataFim?.ToString("dd/MM/yyyy HH:mm") ?? "-")</p>
<p>Status: @Model.Status</p>

<h2>Conferência com a nota</h2>
<table>
    <thead>
        <tr><th>Produto</th><th>Na nota</th><th>Bipadas</th><th>Diferença</th></tr>
    </thead>
    <tbody>
        @foreach (var l in linhas)
        {
            <tr>
                <td>@l.Produto</td>
                <td>@l.NaNota</td>
                <td>@l.Bipadas</td>
                <td>@l.Diferenca</td>
            </tr>
        }
    </tbody>
</table>

@if (Model.AprovadoPor != null)
{
    <p>Aprovado por: @Model.AprovadoPor.Nome</p>
    <p>Justificativa: @Model.Justificativa</p>
}

@if (Model.Status == StatusRecebimento.AguardandoAprovacao)
{
    <h2>Aprovar entrada com diferença</h2>
    <form asp-action="Aprovar" asp-route-id="@Model.Id" method="post">
        <p>
            <label for="justificativa">Justificativa</label><br />
            <textarea id="justificativa" name="justificativa" rows="3" required></textarea>
        </p>
        <button type="submit">Aprovar</button>
    </form>
}

<p><a asp-action="Index">Voltar</a></p>
```

## Testar (como `gerente`)
- [ ] a lista mostra a nota 4521 `Finalizado` (30 de 30) e a nota 7788 `AguardandoAprovacao` (8 de 10)
- [ ] os detalhes da 7788 mostram o Produto D com diferença **-2**
- [ ] aprovar com justificativa muda o status para `Finalizado` e mostra quem aprovou
- [ ] o formulário de aprovação some depois de aprovar
