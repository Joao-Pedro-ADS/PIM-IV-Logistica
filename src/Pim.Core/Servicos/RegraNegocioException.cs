namespace Pim.Core.Servicos;

// Lançada quando uma regra de negócio (RN) é violada
public class RegraNegocioException : Exception
{
    public RegraNegocioException(string mensagem) : base(mensagem) { }
}
