namespace Pim.Core.Entidades;

public class Caixa
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";      // RN01: código único da etiqueta (ex.: CX00000001)

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public int RecebimentoId { get; set; }
    public Recebimento? Recebimento { get; set; }

    // preenchidos quando a caixa é bipada no carregamento
    public int? PedidoVendaId { get; set; }
    public PedidoVenda? PedidoVenda { get; set; }
    public int? CarregamentoId { get; set; }
    public Carregamento? Carregamento { get; set; }

    public StatusCaixa Status { get; set; } = StatusCaixa.NoArmazem;

    public List<MovimentacaoCaixa> Movimentacoes { get; set; } = new();
}
