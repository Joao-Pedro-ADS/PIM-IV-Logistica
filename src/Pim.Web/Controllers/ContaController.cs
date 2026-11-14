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
