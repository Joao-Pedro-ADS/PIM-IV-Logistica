namespace Pim.Core.Entidades;

// RN12: registra veículo, motorista e horário na saída e na volta
public class RegistroPortaria
{
    public int Id { get; set; }
    public TipoRegistroPortaria Tipo { get; set; }
    public DateTime DataHora { get; set; }

    // veículo da frota (saída/volta)
    public int? VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }
    public int? CarregamentoId { get; set; }
    public Carregamento? Carregamento { get; set; }

    // veículo de fornecedor não tem cadastro: guarda só a placa
    public string? PlacaFornecedor { get; set; }

    public int PorteiroId { get; set; }
    public Usuario? Porteiro { get; set; }
}
