# 05 — Login, perfis e menu (≈ 35 min)

**Objetivo (RF01):** só entra quem tem login. Cada perfil vê só o seu menu. Os perfis do app (conferente, operador, porteiro, motorista) **não entram no Web**.

## 1. `Program.cs` completo

Substitua todo o `src/Pim.Web/Program.cs`:

**`src/Pim.Web/Program.cs`**

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados
builder.Services.AddDbContext<PimDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Pim")));

// Login com cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Conta/Login";
        o.AccessDeniedPath = "/Conta/AcessoNegado";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AuthorizeFilter());                        // toda página exige login
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());  // proteção contra CSRF nos formulários
});

var app = builder.Build();

// Cria/atualiza o banco e coloca os dados de teste
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PimDbContext>();
    db.Database.Migrate();
    DadosIniciais.Popular(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
```

O que mudou em relação ao passo anterior:
- `AddAuthentication(...).AddCookie(...)`: o login fica guardado num cookie;
- `AuthorizeFilter`: **toda página exige login**, menos as que tiverem `[AllowAnonymous]`;
- `AutoValidateAntiforgeryTokenAttribute`: protege todos os formulários contra CSRF;
- `UseAuthentication()` antes de `UseAuthorization()`.

## 2. Controller de conta

Crie `src/Pim.Web/Controllers/ContaController.cs`:

**`src/Pim.Web/Controllers/ContaController.cs`**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Web.Controllers;

[AllowAnonymous]
public class ContaController : Controller
{
    private readonly PimDbContext _db;

    public ContaController(PimDbContext db) => _db = db;

    // perfis que usam o sistema Web; os outros usam o aplicativo
    private static readonly Perfil[] PerfisWeb = { Perfil.Gerente, Perfil.Programador, Perfil.Frota, Perfil.Comercial };

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string login, string senha)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Login == login && u.Ativo);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash))
        {
            ViewBag.Erro = "Login ou senha inválidos.";
            return View();
        }
        if (!PerfisWeb.Contains(usuario.Perfil))
        {
            ViewBag.Erro = "Este perfil usa o aplicativo de celular.";
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Perfil.ToString())
        };
        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade));

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AcessoNegado() => View();
}
```

> A senha nunca é comparada como texto: `BCrypt.Verify` compara com o hash guardado no banco.
> O **perfil** vira uma *role*, e é ela que o `[Authorize(Roles = "...")]` dos próximos passos confere.

## 3. Telas

Crie a pasta `src/Pim.Web/Views/Conta/` com dois arquivos.

**`src/Pim.Web/Views/Conta/Login.cshtml`**

```cshtml
@{
    ViewData["Title"] = "Entrar";
}

<h1>Entrar</h1>

@if (ViewBag.Erro != null)
{
    <p>@ViewBag.Erro</p>
}

<form asp-action="Login" method="post">
    <p>
        <label for="login">Login</label><br />
        <input id="login" name="login" required autofocus />
    </p>
    <p>
        <label for="senha">Senha</label><br />
        <input id="senha" name="senha" type="password" required />
    </p>
    <button type="submit">Entrar</button>
</form>
```

**`src/Pim.Web/Views/Conta/AcessoNegado.cshtml`**

```cshtml
@{
    ViewData["Title"] = "Acesso negado";
}

<h1>Acesso negado</h1>
<p>Seu perfil não tem permissão para acessar esta página.</p>
<p><a asp-controller="Home" asp-action="Index">Voltar ao início</a></p>
```

## 4. Imports das views

Substitua `src/Pim.Web/Views/_ViewImports.cshtml`:

**`src/Pim.Web/Views/_ViewImports.cshtml`**

```cshtml
@using Pim.Web
@using Pim.Web.Models
@using Pim.Core.Entidades
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

## 5. Layout com o menu por perfil

Substitua `src/Pim.Web/Views/Shared/_Layout.cshtml` e **apague** `Views/Shared/_Layout.cshtml.css`:

**`src/Pim.Web/Views/Shared/_Layout.cshtml`**

```cshtml
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - PIM</title>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
</head>
<body>
    <header>
        @if (User.Identity?.IsAuthenticated == true)
        {
            <nav>
                <a asp-controller="Home" asp-action="Index">Início</a>

                @if (User.IsInRole("Gerente") || User.IsInRole("Comercial") || User.IsInRole("Programador"))
                {
                    <a asp-controller="Pedidos" asp-action="Index">Pedidos</a>
                }
                @if (User.IsInRole("Gerente") || User.IsInRole("Programador"))
                {
                    <a asp-controller="Carregamentos" asp-action="Index">Carregamentos</a>
                }
                @if (User.IsInRole("Gerente"))
                {
                    <a asp-controller="Recebimentos" asp-action="Index">Recebimentos</a>
                    <a asp-controller="Caixas" asp-action="Index">Rastrear caixa</a>
                }
                @if (User.IsInRole("Gerente") || User.IsInRole("Frota") || User.IsInRole("Programador"))
                {
                    <a asp-controller="Veiculos" asp-action="Index">Veículos</a>
                }
                @if (User.IsInRole("Gerente") || User.IsInRole("Frota"))
                {
                    <a asp-controller="Motoristas" asp-action="Index">Motoristas</a>
                }

                <span>@User.Identity.Name</span>
                <form asp-controller="Conta" asp-action="Sair" method="post">
                    <button type="submit">Sair</button>
                </form>
            </nav>
        }
    </header>

    <main>
        @if (TempData["Mensagem"] != null) { <p>@TempData["Mensagem"]</p> }
        @if (TempData["Erro"] != null) { <p>@TempData["Erro"]</p> }

        @RenderBody()
    </main>

    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

> Os links do menu apontam para telas que ainda não existem e vão ser criadas nos próximos passos. Até lá, clicar neles dá erro 404, e isso é esperado.

## 6. Liberar a página de erro

Em `src/Pim.Web/Controllers/HomeController.cs`, coloque `[AllowAnonymous]` em cima do método `Error()` e adicione `using Microsoft.AspNetCore.Authorization;` no topo.

## Testar
- [ ] abrir o site redireciona para `/Conta/Login`
- [ ] `gerente` / `123456` entra e mostra todos os itens do menu
- [ ] `comercial` entra e vê só **Início** e **Pedidos**
- [ ] `porteiro` mostra "Este perfil usa o aplicativo de celular."
- [ ] senha errada mostra "Login ou senha inválidos."
- [ ] **Sair** volta para o login
