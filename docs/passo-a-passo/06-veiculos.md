# 06 — Veículos, manutenção e QR Code (≈ 35 min)

**Objetivo:** cadastrar veículos (**RF02**), colocar e tirar da manutenção (**RF17**), ver a situação de cada um (**RF18**) e imprimir o QR Code que o porteiro e o operador vão escanear.

| Regra | Onde |
|---|---|
| RN09 limite de caixas | campo `LimiteCaixas` |
| RN16 situação | Disponível / Em rota / Em manutenção |
| RN17 manutenção | o botão alterna; veículo em rota não pode ir para manutenção |

## 1. Controller

Crie `src/Pim.Web/Controllers/VeiculosController.cs`:

**`src/Pim.Web/Controllers/VeiculosController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;
using QRCoder;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente,Frota,Programador")]
public class VeiculosController : Controller
{
    private readonly PimDbContext _db;

    public VeiculosController(PimDbContext db) => _db = db;

    // RF18: situação de cada veículo
    public async Task<IActionResult> Index()
    {
        var veiculos = await _db.Veiculos.OrderBy(v => v.Placa).ToListAsync();
        return View(veiculos);
    }

    // RF02: cadastrar
    [Authorize(Roles = "Gerente,Frota")]
    public IActionResult Criar() => View(new Veiculo());

    [HttpPost]
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Criar(Veiculo veiculo)
    {
        veiculo.Placa = (veiculo.Placa ?? "").Trim().ToUpper();

        if (await _db.Veiculos.AnyAsync(v => v.Placa == veiculo.Placa))
            ModelState.AddModelError(nameof(Veiculo.Placa), "Já existe um veículo com essa placa.");

        if (!ModelState.IsValid)
            return View(veiculo);

        veiculo.Situacao = SituacaoVeiculo.Disponivel;
        _db.Veiculos.Add(veiculo);
        await _db.SaveChangesAsync();
        TempData["Mensagem"] = "Veículo cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Editar(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();
        return View(veiculo);
    }

    [HttpPost]
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Editar(int id, Veiculo dados)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();

        dados.Placa = (dados.Placa ?? "").Trim().ToUpper();
        if (await _db.Veiculos.AnyAsync(v => v.Placa == dados.Placa && v.Id != id))
            ModelState.AddModelError(nameof(Veiculo.Placa), "Já existe um veículo com essa placa.");

        if (!ModelState.IsValid)
            return View(dados);

        // copia só os campos do formulário (a situação não muda aqui)
        veiculo.Placa = dados.Placa;
        veiculo.Modelo = dados.Modelo;
        veiculo.LimiteCaixas = dados.LimiteCaixas;
        await _db.SaveChangesAsync();
        TempData["Mensagem"] = "Veículo atualizado.";
        return RedirectToAction(nameof(Index));
    }

    // RF17: colocar/tirar da manutenção
    [HttpPost]
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> AlternarManutencao(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();

        if (veiculo.Situacao == SituacaoVeiculo.EmRota)
        {
            TempData["Erro"] = "Veículo em rota não pode ir para manutenção.";
            return RedirectToAction(nameof(Index));
        }

        veiculo.Situacao = veiculo.Situacao == SituacaoVeiculo.EmManutencao
            ? SituacaoVeiculo.Disponivel
            : SituacaoVeiculo.EmManutencao;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // RF02: página da etiqueta para imprimir
    [Authorize(Roles = "Gerente,Frota")]
    public async Task<IActionResult> Etiqueta(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();
        return View(veiculo);
    }

    // RF02: imagem do QR Code (o app lê "VEICULO:<id>")
    [Authorize(Roles = "Gerente,Frota")]
    public IActionResult QrCode(int id)
    {
        using var gerador = new QRCodeGenerator();
        using var dados = gerador.CreateQrCode($"VEICULO:{id}", QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(dados).GetGraphic(10);
        return File(png, "image/png");
    }
}
```

> **Por que o `Editar` não salva o objeto recebido direto?** Porque ele copia só os campos do formulário. Assim ninguém consegue mudar a `Situacao` mandando um campo a mais.
> **QR Code:** o conteúdo é `VEICULO:<id>`. O app vai ler isso e buscar o veículo pelo ID.

## 2. Telas

Crie a pasta `src/Pim.Web/Views/Veiculos/`:

**`src/Pim.Web/Views/Veiculos/Index.cshtml`**

```cshtml
@model List<Veiculo>
@{
    ViewData["Title"] = "Veículos";
    var podeEditar = User.IsInRole("Gerente") || User.IsInRole("Frota");
}

<h1>Veículos</h1>

@if (podeEditar)
{
    <p><a asp-action="Criar">Novo veículo</a></p>
}

<table>
    <thead>
        <tr>
            <th>Placa</th>
            <th>Modelo</th>
            <th>Limite de caixas</th>
            <th>Situação</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var v in Model)
        {
            <tr>
                <td>@v.Placa</td>
                <td>@v.Modelo</td>
                <td>@v.LimiteCaixas</td>
                <td>@v.Situacao</td>
                <td>
                    @if (podeEditar)
                    {
                        <a asp-action="Editar" asp-route-id="@v.Id">Editar</a>
                        <a asp-action="Etiqueta" asp-route-id="@v.Id">QR Code</a>
                        <form asp-action="AlternarManutencao" asp-route-id="@v.Id" method="post">
                            <button type="submit">
                                @(v.Situacao == SituacaoVeiculo.EmManutencao ? "Tirar da manutenção" : "Colocar em manutenção")
                            </button>
                        </form>
                    }
                </td>
            </tr>
        }
    </tbody>
</table>
```

**`src/Pim.Web/Views/Veiculos/Criar.cshtml`**

```cshtml
@model Veiculo
@{
    ViewData["Title"] = "Novo veículo";
}

<h1>Novo veículo</h1>

<form asp-action="Criar" method="post">
    <div asp-validation-summary="All"></div>

    <p>
        <label asp-for="Placa">Placa (sem traço)</label><br />
        <input asp-for="Placa" />
    </p>
    <p>
        <label asp-for="Modelo">Modelo</label><br />
        <input asp-for="Modelo" />
    </p>
    <p>
        <label asp-for="LimiteCaixas">Limite de caixas</label><br />
        <input asp-for="LimiteCaixas" />
    </p>

    <button type="submit">Salvar</button>
    <a asp-action="Index">Voltar</a>
</form>
```

**`src/Pim.Web/Views/Veiculos/Editar.cshtml`**

```cshtml
@model Veiculo
@{
    ViewData["Title"] = "Editar veículo";
}

<h1>Editar veículo</h1>

<form asp-action="Editar" asp-route-id="@Model.Id" method="post">
    <div asp-validation-summary="All"></div>

    <p>
        <label asp-for="Placa">Placa (sem traço)</label><br />
        <input asp-for="Placa" />
    </p>
    <p>
        <label asp-for="Modelo">Modelo</label><br />
        <input asp-for="Modelo" />
    </p>
    <p>
        <label asp-for="LimiteCaixas">Limite de caixas</label><br />
        <input asp-for="LimiteCaixas" />
    </p>

    <button type="submit">Salvar</button>
    <a asp-action="Index">Voltar</a>
</form>
```

**`src/Pim.Web/Views/Veiculos/Etiqueta.cshtml`**

```cshtml
@model Veiculo
@{
    ViewData["Title"] = "Etiqueta do veículo";
}

<h1>Etiqueta do veículo</h1>

<p>Placa: @Model.Placa</p>
<img src="@Url.Action("QrCode", new { id = Model.Id })" alt="QR Code do veículo @Model.Placa" width="250" height="250" />

<p>
    <button type="button" onclick="window.print()">Imprimir</button>
    <a asp-action="Index">Voltar</a>
</p>
```

## Testar (como `gerente` ou `frota`)
- [ ] a lista mostra os 3 veículos, com o Truck em `EmManutencao`
- [ ] cadastrar a placa `ABC1D23` de novo mostra "Já existe um veículo com essa placa."
- [ ] salvar o formulário vazio mostra as mensagens de validação
- [ ] cadastrar uma placa nova funciona
- [ ] **Colocar em manutenção** / **Tirar da manutenção** alterna a situação
- [ ] **QR Code** mostra a imagem, e o **Imprimir** abre a impressão
- [ ] como `programador`: vê a lista, mas sem os botões
