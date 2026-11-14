namespace Pim.Core.Entidades;

public class PedidoVendaItem
{
    public int Id { get; set; }
    public int PedidoVendaId { get; set; }
    public PedidoVenda? PedidoVenda { get; set; }
    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    public int Quantidade { get; set; }
}
