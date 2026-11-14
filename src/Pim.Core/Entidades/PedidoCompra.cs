namespace Pim.Core.Entidades;

public class PedidoCompra
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public DateTime DataEmissao { get; set; }

    public List<PedidoCompraItem> Itens { get; set; } = new();
}
