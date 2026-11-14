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
