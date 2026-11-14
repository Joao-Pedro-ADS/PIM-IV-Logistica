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
