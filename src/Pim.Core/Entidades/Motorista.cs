using System.ComponentModel.DataAnnotations;

namespace Pim.Core.Entidades;

public class Motorista
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome.")]
    public string Nome { get; set; } = "";

    [Required(ErrorMessage = "Informe o CPF.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "O CPF tem 11 números, sem pontos e traço.")]
    public string Cpf { get; set; } = "";

    [Required(ErrorMessage = "Informe o número da CNH.")]
    public string NumeroCnh { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime ValidadeCnh { get; set; }

    // login do motorista no aplicativo
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public bool CnhValidaEm(DateTime data) => ValidadeCnh.Date >= data.Date;   // RN18
}
