using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;
using Pim.Core.Servicos;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente,Comercial,Programador")]
public class PedidosController : Controller
{
    private readonly PimDbContext _db;
    private readonly PedidoService _pedidoService;

    public PedidosController(PimDbContext db, PedidoService pedidoService)
    {
        _db = db;
        _pedidoService = pedidoService;
    }

    // RF08: lista com filtro por status e busca por número/cliente
    public async Task<IActionResult> Index(StatusPedido? status, string? busca)
    {
        var consulta = _db.PedidosVenda
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
            .AsQueryable();

        if (status != null)
            consulta = consulta.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(p => p.Cliente!.Nome.Contains(busca) || p.Numero.ToString() == busca);

        ViewBag.Status = status;
        ViewBag.Busca = busca;
        return View(await consulta.OrderByDescending(p => p.DataEmissao).ToListAsync());
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var pedido = await _db.PedidosVenda
            .Include(p => p.Cliente)
            .Include(p => p.Endereco)
            .Include(p => p.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null) return NotFound();

        // em quais carregamentos o pedido já esteve
        ViewBag.Carregamentos = await _db.CarregamentoPedidos
            .Include(cp => cp.Carregamento).ThenInclude(c => c!.Veiculo)
            .Where(cp => cp.PedidoVendaId == id)
            .ToListAsync();

        return View(pedido);
    }

    // RF09
    [HttpPost]
    [Authorize(Roles = "Gerente")]
    public async Task<IActionResult> Cancelar(int id)
    {
        try
        {
            await _pedidoService.CancelarAsync(id);
            TempData["Mensagem"] = "Pedido cancelado.";
        }
        catch (RegraNegocioException ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }
}
