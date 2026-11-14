namespace Pim.Core.Entidades;

public class Recebimento
{
    public int Id { get; set; }
    public int NotaFiscalId { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }

    public int ConferenteId { get; set; }
    public Usuario? Conferente { get; set; }

    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public StatusRecebimento Status { get; set; } = StatusRecebimento.EmAndamento;

    // RN03: só o gerente aceita entrada com diferença
    public int? AprovadoPorId { get; set; }
    public Usuario? AprovadoPor { get; set; }
    public string? Justificativa { get; set; }

    public List<Caixa> Caixas { get; set; } = new();
}
