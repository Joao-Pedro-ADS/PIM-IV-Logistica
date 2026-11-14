using System.ComponentModel.DataAnnotations;

namespace Pim.Core.Entidades;

public class Veiculo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe a placa.")]
    [StringLength(7, MinimumLength = 7, ErrorMessage = "A placa tem 7 caracteres, sem traço.")]
    public string Placa { get; set; } = "";

    [Required(ErrorMessage = "Informe o modelo.")]
    public string Modelo { get; set; } = "";

    [Range(1, 1000, ErrorMessage = "O limite deve ser entre 1 e 1000 caixas.")]
    public int LimiteCaixas { get; set; }                       // RN09

    public SituacaoVeiculo Situacao { get; set; } = SituacaoVeiculo.Disponivel;   // RN16
}
