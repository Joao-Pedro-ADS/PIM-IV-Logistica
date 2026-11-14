namespace Pim.Core.Entidades;

public class Produto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";      // código de barras do fornecedor (EAN)
    public string Descricao { get; set; } = "";
}
