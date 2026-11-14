using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

public record RetornoCaixa(string Codigo, int PedidoNumero, int CaixasFaltando, bool PedidoReaberto);

// RF15 / RN14: as caixas que voltaram são bipadas e o pedido volta para Aberto
public class RetornoService
{
    private readonly PimDbContext _db;

    public RetornoService(PimDbContext db) => _db = db;

    public async Task<RetornoCaixa> BiparAsync(string codigoCaixa, int conferenteId)
    {
        codigoCaixa = (codigoCaixa ?? "").Trim().ToUpper();

        var caixa = await _db.Caixas
            .Include(c => c.PedidoVenda)
            .Include(c => c.Carregamento)
            .FirstOrDefaultAsync(c => c.Codigo == codigoCaixa)
            ?? throw new RegraNegocioException("Caixa não encontrada.");

        if (caixa.Status != StatusCaixa.EmRota || caixa.PedidoVenda == null || caixa.Carregamento == null)
            throw new RegraNegocioException("Esta caixa não está em rota.");
        if (caixa.PedidoVenda.Status != StatusPedido.NaoEntregue)
            throw new RegraNegocioException("O pedido desta caixa não está como não entregue.");
        if (caixa.Carregamento.DataVolta == null)
            throw new RegraNegocioException("O veículo desta caixa ainda não voltou.");

        var pedido = caixa.PedidoVenda;

        // histórico: guarda de qual carregamento a caixa voltou
        _db.MovimentacoesCaixa.Add(new MovimentacaoCaixa
        {
            CaixaId = caixa.Id, Tipo = TipoMovimentacao.Retorno, DataHora = DateTime.Now,
            UsuarioId = conferenteId, CarregamentoId = caixa.CarregamentoId
        });

        // a caixa fica livre no armazém para um novo carregamento
        caixa.Status = StatusCaixa.NoArmazem;
        caixa.PedidoVendaId = null;
        caixa.CarregamentoId = null;

        // RN14: quando a última caixa do pedido volta, o pedido fica Aberto de novo
        int faltando = await _db.Caixas.CountAsync(c =>
            c.PedidoVendaId == pedido.Id && c.Status == StatusCaixa.EmRota && c.Id != caixa.Id);
        if (faltando == 0)
            pedido.Status = StatusPedido.Aberto;

        await _db.SaveChangesAsync();
        return new RetornoCaixa(caixa.Codigo, pedido.Numero, faltando, faltando == 0);
    }
}
