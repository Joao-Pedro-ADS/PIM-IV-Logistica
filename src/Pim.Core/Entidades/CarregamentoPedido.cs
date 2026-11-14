namespace Pim.Core.Entidades;

// Pedidos do carregamento + resultado da entrega (RN13)
public class CarregamentoPedido
{
    public int Id { get; set; }
    public int CarregamentoId { get; set; }
    public Carregamento? Carregamento { get; set; }
    public int PedidoVendaId { get; set; }
    public PedidoVenda? PedidoVenda { get; set; }

    public ResultadoEntrega Resultado { get; set; } = ResultadoEntrega.Pendente;
    public string? Motivo { get; set; }
    public DateTime? DataRegistro { get; set; }
}
