namespace Pim.Core.Entidades;

public class PedidoVenda
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int EnderecoId { get; set; }              // endereço de entrega
    public Endereco? Endereco { get; set; }
    public DateTime DataEmissao { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.Aberto;

    public List<PedidoVendaItem> Itens { get; set; } = new();

    // não vai para o banco: só funciona com .Include(p => p.Itens)
    public int TotalCaixas => Itens.Sum(i => i.Quantidade);
}
