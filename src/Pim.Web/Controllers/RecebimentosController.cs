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
