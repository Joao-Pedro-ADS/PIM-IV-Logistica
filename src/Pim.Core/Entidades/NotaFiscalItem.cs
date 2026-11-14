namespace Pim.Core.Entidades;

public class NotaFiscalItem
{
    public int Id { get; set; }
    public int NotaFiscalId { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }
    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    public int Quantidade { get; set; }
}
