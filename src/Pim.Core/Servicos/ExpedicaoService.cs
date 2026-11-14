using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

// RF11: o operador bipa o QR do veículo e as caixas que sobem nele
public class ExpedicaoService
{
    private readonly PimDbContext _db;

    public ExpedicaoService(PimDbContext db) => _db = db;

    private IQueryable<Carregamento> ComTudo() => _db.Carregamentos
        .Include(c => c.Veiculo)
        .Include(c => c.Motorista)
        .Include(c => c.Caixas)
        .Include(c => c.Pedidos).ThenInclude(cp => cp.PedidoVenda).ThenInclude(p => p!.Itens).ThenInclude(i => i.Produto);

    // Carregamento planejado do veículo do QR Code
    public async Task<ResumoCarregamento> BuscarPorVeiculoAsync(string qr)
    {
        int veiculoId = RegrasCarregamento.LerQrVeiculo(qr);

        var carregamento = await ComTudo()
            .FirstOrDefaultAsync(c => c.VeiculoId == veiculoId && c.Status == StatusCarregamento.Planejado)
            ?? throw new RegraNegocioException("Este veículo não tem carregamento planejado.");

        return RegrasCarregamento.Resumir(carregamento);
    }

    // RN10: caixa que não pertence ao carregamento é recusada
    public async Task<ResumoCarregamento> BiparCaixaAsync(int carregamentoId, string codigoCaixa, int operadorId)
    {
        var carregamento = await ComTudo().FirstOrDefaultAsync(c => c.Id == carregamentoId)
            ?? throw new RegraNegocioException("Carregamento não encontrado.");

        if (carregamento.Status != StatusCarregamento.Planejado)
            throw new RegraNegocioException("Este carregamento não está mais em montagem.");

        codigoCaixa = (codigoCaixa ?? "").Trim().ToUpper();
        var caixa = await _db.Caixas.FirstOrDefaultAsync(c => c.Codigo == codigoCaixa)
            ?? throw new RegraNegocioException("Caixa não encontrada.");

        if (caixa.CarregamentoId == carregamento.Id)
            throw new RegraNegocioException("Esta caixa já foi bipada neste carregamento.");
        if (caixa.CarregamentoId != null)
            throw new RegraNegocioException("RN10: esta caixa pertence a outro carregamento.");
        if (caixa.Status != StatusCaixa.NoArmazem)
            throw new RegraNegocioException("RN10: esta caixa não está disponível no armazém.");

        // primeiro pedido do carregamento que ainda precisa de uma caixa deste produto
        var codigoProduto = await _db.Produtos.Where(p => p.Id == caixa.ProdutoId).Select(p => p.Codigo).FirstAsync();
        var falta = RegrasCarregamento.Faltando(carregamento)
            .FirstOrDefault(f => f.CodigoProduto == codigoProduto)
            ?? throw new RegraNegocioException("RN10: esta caixa não pertence ao carregamento.");

        var pedido = carregamento.Pedidos.First(cp => cp.PedidoVenda!.Numero == falta.PedidoNumero).PedidoVenda!;

        caixa.CarregamentoId = carregamento.Id;
        caixa.PedidoVendaId = pedido.Id;
        caixa.Status = StatusCaixa.Carregada;
        carregamento.Caixas.Add(caixa);
        _db.MovimentacoesCaixa.Add(new MovimentacaoCaixa
        {
            CaixaId = caixa.Id,
            Tipo = TipoMovimentacao.Carregamento,
            DataHora = DateTime.Now,
            UsuarioId = operadorId,
            CarregamentoId = carregamento.Id
        });

        await _db.SaveChangesAsync();
        return RegrasCarregamento.Resumir(carregamento);
    }
}
