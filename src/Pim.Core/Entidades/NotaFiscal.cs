namespace Pim.Core.Entidades;

public class NotaFiscal
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public string Chave { get; set; } = "";       // 44 dígitos (código de barras do DANFE)
    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public int? PedidoCompraId { get; set; }
    public PedidoCompra? PedidoCompra { get; set; }
    public DateTime DataEmissao { get; set; }
    public int QuantidadeVolumes { get; set; }

    public List<NotaFiscalItem> Itens { get; set; } = new();
}
