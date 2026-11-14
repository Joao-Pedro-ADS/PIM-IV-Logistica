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
