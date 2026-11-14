using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

public class PedidoService
{
    private readonly PimDbContext _db;

    public PedidoService(PimDbContext db) => _db = db;

    // RF09 / RN07: só cancela antes de o veículo sair
    public async Task CancelarAsync(int pedidoId)
    {
        var pedido = await _db.PedidosVenda.FindAsync(pedidoId)
            ?? throw new RegraNegocioException("Pedido não encontrado.");

        if (pedido.Status != StatusPedido.Aberto && pedido.Status != StatusPedido.EmCarregamento)
            throw new RegraNegocioException("RN07: o pedido só pode ser cancelado antes de o veículo sair.");

        // tira o pedido do carregamento; se o carregamento ficar vazio, ele é cancelado
        var vinculos = await _db.CarregamentoPedidos
            .Include(cp => cp.Carregamento).ThenInclude(c => c!.Pedidos)
            .Where(cp => cp.PedidoVendaId == pedidoId && cp.Carregamento!.Status == StatusCarregamento.Planejado)
            .ToListAsync();
        foreach (var vinculo in vinculos)
        {
            if (vinculo.Carregamento!.Pedidos.Count == 1)
                vinculo.Carregamento.Status = StatusCarregamento.Cancelado;
            _db.CarregamentoPedidos.Remove(vinculo);
        }

        // as caixas que já tinham sido bipadas voltam a ficar livres no armazém
        var caixas = await _db.Caixas.Where(c => c.PedidoVendaId == pedidoId).ToListAsync();
        foreach (var caixa in caixas)
        {
            caixa.PedidoVendaId = null;
            caixa.CarregamentoId = null;
            caixa.Status = StatusCaixa.NoArmazem;
        }

        pedido.Status = StatusPedido.Cancelado;
        await _db.SaveChangesAsync();
    }
}
