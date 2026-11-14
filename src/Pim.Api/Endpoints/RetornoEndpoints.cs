using System.Security.Claims;
using Pim.Core.Servicos;

namespace Pim.Api.Endpoints;

public static class RetornoEndpoints
{
    public static void MapRetornos(this WebApplication app)
    {
        var grupo = app.MapGroup("/retornos").WithTags("Retorno (conferente)")
            .RequireAuthorization(p => p.RequireRole("Conferente"));

        // RF15 / RN14: bipa a caixa que voltou; na última, o pedido volta para Aberto
        grupo.MapPost("/caixas", async (BiparCaixaRequest dados, RetornoService retorno, ClaimsPrincipal user) =>
            Results.Ok(await retorno.BiparAsync(dados.CodigoCaixa, user.UsuarioId())));
    }
}
