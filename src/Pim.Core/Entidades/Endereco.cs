namespace Pim.Core.Entidades;

public class Endereco
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string Logradouro { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cep { get; set; } = "";

    public override string ToString() => $"{Logradouro}, {Numero} - {Bairro}, {Cidade}/{Uf}";
}
