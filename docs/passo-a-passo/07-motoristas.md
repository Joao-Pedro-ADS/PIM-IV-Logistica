# 07 — Motoristas (≈ 25 min)

**Objetivo (RF03):** cadastrar motoristas com CNH e validade. A lista mostra se a CNH está **válida, vencendo ou vencida** (RN18) e mascara o CPF (LGPD).

## 1. Controller

Crie `src/Pim.Web/Controllers/MotoristasController.cs`:

**`src/Pim.Web/Controllers/MotoristasController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente,Frota")]
public class MotoristasController : Controller
{
    private readonly PimDbContext _db;

    public MotoristasController(PimDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var motoristas = await _db.Motoristas.Include(m => m.Usuario).OrderBy(m => m.Nome).ToListAsync();
        return View(motoristas);
    }

    // RF03: cadastrar
    public async Task<IActionResult> Criar()
    {
        await CarregarUsuarios();
        return View(new Motorista { ValidadeCnh = DateTime.Today });
    }

    [HttpPost]
    public async Task<IActionResult> Criar(Motorista motorista)
    {
        if (await _db.Motoristas.AnyAsync(m => m.Cpf == motorista.Cpf))
            ModelState.AddModelError(nameof(Motorista.Cpf), "Já existe um motorista com esse CPF.");

        if (!ModelState.IsValid)
        {
            await CarregarUsuarios();
            return View(motorista);
        }

        _db.Motoristas.Add(motorista);
        await _db.SaveChangesAsync();
        TempData["Mensagem"] = "Motorista cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var motorista = await _db.Motoristas.FindAsync(id);
        if (motorista == null) return NotFound();
        await CarregarUsuarios();
        return View(motorista);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, Motorista dados)
    {
        var motorista = await _db.Motoristas.FindAsync(id);
        if (motorista == null) return NotFound();

        if (await _db.Motoristas.AnyAsync(m => m.Cpf == dados.Cpf && m.Id != id))
            ModelState.AddModelError(nameof(Motorista.Cpf), "Já existe um motorista com esse CPF.");

        if (!ModelState.IsValid)
        {
            await CarregarUsuarios();
            return View(dados);
        }

        motorista.Nome = dados.Nome;
        motorista.Cpf = dados.Cpf;
        motorista.NumeroCnh = dados.NumeroCnh;
        motorista.ValidadeCnh = dados.ValidadeCnh;
        motorista.UsuarioId = dados.UsuarioId;
        await _db.SaveChangesAsync();
        TempData["Mensagem"] = "Motorista atualizado.";
        return RedirectToAction(nameof(Index));
    }

    // lista de logins com perfil Motorista (para o app)
    private async Task CarregarUsuarios()
    {
        var usuarios = await _db.Usuarios.Where(u => u.Perfil == Perfil.Motorista).OrderBy(u => u.Nome).ToListAsync();
        ViewBag.Usuarios = new SelectList(usuarios, nameof(Usuario.Id), nameof(Usuario.Nome));
    }
}
```

> **Login do aplicativo:** liga o motorista a um usuário com perfil Motorista. É assim que o app vai saber quais carregamentos são dele.

## 2. Telas

Crie a pasta `src/Pim.Web/Views/Motoristas/`. O formulário fica num **partial** (`_Formulario`) e é usado no Criar e no Editar.

**`src/Pim.Web/Views/Motoristas/Index.cshtml`**

```cshtml
@model List<Motorista>
@{
    ViewData["Title"] = "Motoristas";
}

<h1>Motoristas</h1>

<p><a asp-action="Criar">Novo motorista</a></p>

<table>
    <thead>
        <tr>
            <th>Nome</th>
            <th>CPF</th>
            <th>CNH</th>
            <th>Validade da CNH</th>
            <th>Situação da CNH</th>
            <th>Login do app</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var m in Model)
        {
            var dias = (m.ValidadeCnh.Date - DateTime.Today).Days;
            var situacaoCnh = dias < 0 ? "Vencida" : dias <= 30 ? $"Vence em {dias} dias" : "Válida";
            // LGPD: mostra só parte do CPF
            var cpfMascarado = $"***.{m.Cpf.Substring(3, 3)}.{m.Cpf.Substring(6, 3)}-**";

            <tr>
                <td>@m.Nome</td>
                <td>@cpfMascarado</td>
                <td>@m.NumeroCnh</td>
                <td>@m.ValidadeCnh.ToString("dd/MM/yyyy")</td>
                <td>@situacaoCnh</td>
                <td>@(m.Usuario?.Nome ?? "-")</td>
                <td><a asp-action="Editar" asp-route-id="@m.Id">Editar</a></td>
            </tr>
        }
    </tbody>
</table>
```

**`src/Pim.Web/Views/Motoristas/_Formulario.cshtml`**

```cshtml
@model Motorista

<div asp-validation-summary="All"></div>

<p>
    <label asp-for="Nome">Nome</label><br />
    <input asp-for="Nome" />
</p>
<p>
    <label asp-for="Cpf">CPF (só números)</label><br />
    <input asp-for="Cpf" />
</p>
<p>
    <label asp-for="NumeroCnh">Número da CNH</label><br />
    <input asp-for="NumeroCnh" />
</p>
<p>
    <label asp-for="ValidadeCnh">Validade da CNH</label><br />
    <input asp-for="ValidadeCnh" />
</p>
<p>
    <label asp-for="UsuarioId">Login do aplicativo</label><br />
    <select asp-for="UsuarioId" asp-items="(SelectList)ViewBag.Usuarios">
        <option value="">Nenhum</option>
    </select>
</p>
```

**`src/Pim.Web/Views/Motoristas/Criar.cshtml`**

```cshtml
@model Motorista
@{
    ViewData["Title"] = "Novo motorista";
}

<h1>Novo motorista</h1>

<form asp-action="Criar" method="post">
    <partial name="_Formulario" model="Model" />
    <button type="submit">Salvar</button>
    <a asp-action="Index">Voltar</a>
</form>
```

**`src/Pim.Web/Views/Motoristas/Editar.cshtml`**

```cshtml
@model Motorista
@{
    ViewData["Title"] = "Editar motorista";
}

<h1>Editar motorista</h1>

<form asp-action="Editar" asp-route-id="@Model.Id" method="post">
    <partial name="_Formulario" model="Model" />
    <button type="submit">Salvar</button>
    <a asp-action="Index">Voltar</a>
</form>
```

## Testar (como `gerente` ou `frota`)
- [ ] Pedro Costa aparece como **Vencida** e João Ferreira como **Vence em 20 dias**
- [ ] o CPF aparece mascarado (`***.456.789-**`)
- [ ] cadastrar com um CPF repetido mostra o erro
- [ ] editar a validade da CNH do Pedro para o futuro deixa a CNH **Válida**
- [ ] como `programador`: **Acesso negado**
