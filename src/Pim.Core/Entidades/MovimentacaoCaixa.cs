namespace Pim.Core.Entidades;

// Cada bipagem de uma caixa gera uma movimentação (histórico / rastreabilidade)
public class MovimentacaoCaixa
{
    public int Id { get; set; }
    public int CaixaId { get; set; }
    public Caixa? Caixa { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public DateTime DataHora { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int? CarregamentoId { get; set; }
    public Carregamento? Carregamento { get; set; }
}
