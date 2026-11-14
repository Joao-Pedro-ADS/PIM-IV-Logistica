namespace Pim.Core.Entidades;

// RN08: veículo + motorista + data + pedidos
public class Carregamento
{
    public int Id { get; set; }
    public int VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }
    public int MotoristaId { get; set; }
    public Motorista? Motorista { get; set; }
    public DateTime DataPrevista { get; set; }
    public StatusCarregamento Status { get; set; } = StatusCarregamento.Planejado;
    public DateTime? DataSaida { get; set; }
    public DateTime? DataVolta { get; set; }

    public List<CarregamentoPedido> Pedidos { get; set; } = new();
    public List<Caixa> Caixas { get; set; } = new();
}
