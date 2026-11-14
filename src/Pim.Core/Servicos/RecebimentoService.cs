using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

// Uma linha da conferência: produto, quanto a nota diz e quanto foi bipado
public record LinhaConferencia(string Produto, int NaNota, int Bipadas)
{
    public int Diferenca => Bipadas - NaNota;
}

public class RecebimentoService
{
    private readonly PimDbContext _db;

    public RecebimentoService(PimDbContext db) => _db = db;

    // RF07: compara o que foi bipado com a nota
    public async Task<List<LinhaConferencia>> ConferirAsync(int recebimentoId)
    {
        var recebimento = await _db.Recebimentos
            .Include(r => r.NotaFiscal).ThenInclude(n => n!.Itens).ThenInclude(i => i.Produto)
            .Include(r => r.Caixas)
            .FirstOrDefaultAsync(r => r.Id == recebimentoId)
            ?? throw new RegraNegocioException("Recebimento não encontrado.");

        return recebimento.NotaFiscal!.Itens
            .Select(item => new LinhaConferencia(
                item.Produto!.Descricao,
                item.Quantidade,
                recebimento.Caixas.Count(c => c.ProdutoId == item.ProdutoId)))
            .ToList();
    }

    // RN03: só o gerente aceita a entrada com diferença
    public async Task AprovarAsync(int recebimentoId, int gerenteId, string? justificativa)
    {
        var recebimento = await _db.Recebimentos.FindAsync(recebimentoId)
            ?? throw new RegraNegocioException("Recebimento não encontrado.");

        if (recebimento.Status != StatusRecebimento.AguardandoAprovacao)
            throw new RegraNegocioException("Este recebimento não está aguardando aprovação.");
        if (string.IsNullOrWhiteSpace(justificativa))
            throw new RegraNegocioException("Informe a justificativa para aceitar a diferença.");

        recebimento.Status = StatusRecebimento.Finalizado;
        recebimento.AprovadoPorId = gerenteId;
        recebimento.Justificativa = justificativa.Trim();
        recebimento.DataFim = DateTime.Now;
        await _db.SaveChangesAsync();
    }
}
