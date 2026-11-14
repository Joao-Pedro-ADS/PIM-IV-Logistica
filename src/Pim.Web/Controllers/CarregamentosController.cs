using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;
using Pim.Core.Servicos;

namespace Pim.Web.Controllers;

[Authorize(Roles = "Gerente,Programador")]
public class CarregamentosController : Controller
{
    private readonly PimDbContext _db;
    private readonly CarregamentoService _carregamentoService;

    public CarregamentosController(PimDbContext db, CarregamentoService carregamentoService)
    {
        _db = db;
        _carregamentoService = carregamentoService;
    }

    public async Task<IActionResult> Index()
    {
        var carregamentos = await _db.Carregamentos
            .Include(c => c.Veiculo)
            .Include(c => c.Motorista)
            .Include(c => c.Caixas)
            .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Itens)
            .OrderByDescending(c => c.DataPrevista)
            .ToListAsync();
        return View(carregamentos);
    }

    // RF10
    public async Task<IActionResult> Criar()
    {
        await CarregarListas();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Criar(int veiculoId, int motoristaId, DateTime dataPrevista, List<int> pedidoIds)
    {
        try
        {
            var carregamento = await _carregamentoService.CriarAsync(veiculoId, motoristaId, dataPrevista, pedidoIds);
            TempData["Mensagem"] = "Carregamento criado.";
            return RedirectToAction(nameof(Detalhes), new { id = carregamento.Id });
        }
        catch (RegraNegocioException ex)
        {
            ViewBag.Erro = ex.Message;
            await CarregarListas();
            return View();
        }
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var carregamento = await _db.Carregamentos
            .Include(c => c.Veiculo)
            .Include(c => c.Motorista)
            .Include(c => c.Caixas).ThenInclude(cx => cx.Produto)
            .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Cliente)
            .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Itens)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (carregamento == null) return NotFound();
        return View(carregamento);
    }

    // mostra todos os veículos e motoristas: quem não pode ser usado é barrado pelo serviço (RN17, RN18, RN19)
    private async Task CarregarListas()
    {
        ViewBag.Veiculos = await _db.Veiculos.OrderBy(v => v.Placa).ToListAsync();
        ViewBag.Motoristas = await _db.Motoristas.OrderBy(m => m.Nome).ToListAsync();
        ViewBag.Pedidos = await _db.PedidosVenda
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
            .Where(p => p.Status == StatusPedido.Aberto)
            .OrderBy(p => p.DataEmissao)
            .ToListAsync();
    }
}
