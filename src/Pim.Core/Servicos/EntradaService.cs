using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Entidades;

namespace Pim.Core.Servicos;

// O que o conferente vê no app: a nota e a conferência produto a produto
public record DetalheEntrada(int Id, int NotaNumero, string Fornecedor, string Status, List<LinhaConferencia> Linhas);

// RF06 / RF07: entrada das caixas do fornecedor, bipadas pelo conferente no app
public class EntradaService
{
    private readonly PimDbContext _db;
    private readonly RecebimentoService _recebimentoService;

    public EntradaService(PimDbContext db, RecebimentoService recebimentoService)
    {
        _db = db;
        _recebimentoService = recebimentoService;
    }

    public async Task<DetalheEntrada> DetalharAsync(int recebimentoId)
    {
        var recebimento = await _db.Recebimentos
            .Include(r => r.NotaFiscal).ThenInclude(n => n!.Fornecedor)
            .FirstOrDefaultAsync(r => r.Id == recebimentoId)
            ?? throw new RegraNegocioException("Recebimento não encontrado.");

        return new DetalheEntrada(
            recebimento.Id,
            recebimento.NotaFiscal!.Numero,
            recebimento.NotaFiscal.Fornecedor!.Nome,
            recebimento.Status.ToString(),
            await _recebimentoService.ConferirAsync(recebimentoId));
    }

    // O conferente bipa o código de barras do DANFE (chave da nota)
    public async Task<Recebimento> IniciarAsync(string chaveNota, int conferenteId)
    {
        chaveNota = (chaveNota ?? "").Trim();

        var nota = await _db.NotasFiscais.FirstOrDefaultAsync(n => n.Chave == chaveNota)
            ?? throw new RegraNegocioException("Nota fiscal não encontrada.");

        if (await _db.Recebimentos.AnyAsync(r => r.NotaFiscalId == nota.Id))
            throw new RegraNegocioException("Esta nota já tem uma entrada registrada.");

        var recebimento = new Recebimento
        {
            NotaFiscalId = nota.Id,
            ConferenteId = conferenteId,
            DataInicio = DateTime.Now,
            Status = StatusRecebimento.EmAndamento
        };
        _db.Recebimentos.Add(recebimento);
        await _db.SaveChangesAsync();
        return recebimento;
    }

    // RF06: bipa o código de barras do produto e cria a caixa com etiqueta nova
    public async Task<Caixa> BiparAsync(int recebimentoId, string codigoProduto, int conferenteId)
    {
        var recebimento = await _db.Recebimentos
            .Include(r => r.NotaFiscal).ThenInclude(n => n!.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(r => r.Id == recebimentoId)
            ?? throw new RegraNegocioException("Recebimento não encontrado.");

        if (recebimento.Status != StatusRecebimento.EmAndamento)
            throw new RegraNegocioException("Esta entrada já foi finalizada.");

        codigoProduto = (codigoProduto ?? "").Trim();

        // RN02: só entra caixa de produto que está na nota
        var item = recebimento.NotaFiscal!.Itens.FirstOrDefault(i => i.Produto!.Codigo == codigoProduto)
            ?? throw new RegraNegocioException("RN02: este produto não está na nota fiscal.");

        // RN01: cada caixa recebe um código único (CX + 8 números)
        var ultimo = await _db.Caixas.Where(c => c.Codigo.StartsWith("CX")).MaxAsync(c => (string?)c.Codigo);
        int proximo = ultimo == null ? 1 : int.Parse(ultimo[2..]) + 1;

        var caixa = new Caixa
        {
            Codigo = $"CX{proximo:D8}",
            ProdutoId = item.ProdutoId,
            Produto = item.Produto,
            RecebimentoId = recebimento.Id,
            Status = StatusCaixa.NoArmazem,
            Movimentacoes =
            {
                new MovimentacaoCaixa { Tipo = TipoMovimentacao.Entrada, DataHora = DateTime.Now, UsuarioId = conferenteId }
            }
        };
        _db.Caixas.Add(caixa);
        await _db.SaveChangesAsync();
        return caixa;
    }

    // RF07 / RN03: se bater com a nota, finaliza; se não, espera o gerente aprovar no Web
    public async Task<Recebimento> FinalizarAsync(int recebimentoId)
    {
        var recebimento = await _db.Recebimentos.FindAsync(recebimentoId)
            ?? throw new RegraNegocioException("Recebimento não encontrado.");

        if (recebimento.Status != StatusRecebimento.EmAndamento)
            throw new RegraNegocioException("Esta entrada já foi finalizada.");

        var linhas = await _recebimentoService.ConferirAsync(recebimentoId);

        // caixa bipada de produto fora da nota não existe (RN02), então basta olhar as linhas
        if (linhas.All(l => l.Diferenca == 0))
        {
            recebimento.Status = StatusRecebimento.Finalizado;
            recebimento.DataFim = DateTime.Now;
        }
        else
        {
            recebimento.Status = StatusRecebimento.AguardandoAprovacao;
        }

        await _db.SaveChangesAsync();
        return recebimento;
    }
}
