using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

public class CarregamentoService
{
    private readonly PimDbContext _db;

    public CarregamentoService(PimDbContext db) => _db = db;

    // RF10: criar carregamento aplicando as regras
    public async Task<Carregamento> CriarAsync(int veiculoId, int motoristaId, DateTime data, List<int> pedidoIds)
    {
        if (pedidoIds.Count == 0)
            throw new RegraNegocioException("RN08: escolha pelo menos um pedido.");

        var veiculo = await _db.Veiculos.FindAsync(veiculoId)
            ?? throw new RegraNegocioException("Veículo não encontrado.");
        if (veiculo.Situacao == SituacaoVeiculo.EmManutencao)
            throw new RegraNegocioException("RN17: veículo em manutenção não pode ser usado em carregamento.");

        var motorista = await _db.Motoristas.FindAsync(motoristaId)
            ?? throw new RegraNegocioException("Motorista não encontrado.");
        if (!motorista.CnhValidaEm(data))
            throw new RegraNegocioException("RN18: a CNH do motorista estará vencida na data do carregamento.");

        // RN19: um carregamento por vez (em aberto = planejado ou em rota)
        var emAberto = new[] { StatusCarregamento.Planejado, StatusCarregamento.EmRota };
        if (await _db.Carregamentos.AnyAsync(c => c.VeiculoId == veiculoId && emAberto.Contains(c.Status)))
            throw new RegraNegocioException("RN19: este veículo já está em outro carregamento.");
        if (await _db.Carregamentos.AnyAsync(c => c.MotoristaId == motoristaId && emAberto.Contains(c.Status)))
            throw new RegraNegocioException("RN19: este motorista já está em outro carregamento.");

        var pedidos = await _db.PedidosVenda.Include(p => p.Itens)
            .Where(p => pedidoIds.Contains(p.Id))
            .ToListAsync();

        // RN06: o pedido vai inteiro em um único carregamento
        if (pedidos.Any(p => p.Status != StatusPedido.Aberto))
            throw new RegraNegocioException("RN06: um dos pedidos não está aberto ou já está em outro carregamento.");

        // RN09: limite de caixas do veículo
        int totalCaixas = pedidos.Sum(p => p.TotalCaixas);
        if (totalCaixas > veiculo.LimiteCaixas)
            throw new RegraNegocioException($"RN09: são {totalCaixas} caixas e o veículo aceita no máximo {veiculo.LimiteCaixas}.");

        var carregamento = new Carregamento
        {
            Veiculo = veiculo,
            Motorista = motorista,
            DataPrevista = data.Date,
            Status = StatusCarregamento.Planejado
        };
        foreach (var pedido in pedidos)
        {
            pedido.Status = StatusPedido.EmCarregamento;
            carregamento.Pedidos.Add(new CarregamentoPedido { PedidoVenda = pedido });
        }

        _db.Carregamentos.Add(carregamento);
        await _db.SaveChangesAsync();
        return carregamento;
    }
}
