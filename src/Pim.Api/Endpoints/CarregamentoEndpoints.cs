using System.Security.Claims;
using Pim.Core.Servicos;

namespace Pim.Api.Endpoints;

public record BiparCaixaRequest(string CodigoCaixa);

public static class CarregamentoEndpoints
{
    public static void MapCarregamentos(this WebApplication app)
    {
        var grupo = app.MapGroup("/carregamentos").WithTags("Carregamento (operador)")
            .RequireAuthorization(p => p.RequireRole("Operador"));

        // RF11: bipa o QR do veículo e vê o que falta subir
        grupo.MapGet("/veiculo", async (string qr, ExpedicaoService expedicao) =>
            Results.Ok(await expedicao.BuscarPorVeiculoAsync(qr)));

        // RF11 / RN10: bipa uma caixa que sobe no veículo
        grupo.MapPost("/{id:int}/caixas", async (int id, BiparCaixaRequest dados, ExpedicaoService expedicao, ClaimsPrincipal user) =>
            Results.Ok(await expedicao.BiparCaixaAsync(id, dados.CodigoCaixa, user.UsuarioId())));
    }
}
