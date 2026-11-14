using System.Security.Claims;
using Pim.Core.Servicos;

namespace Pim.Api.Endpoints;

public record IniciarEntradaRequest(string ChaveNota);
public record BiparProdutoRequest(string CodigoProduto);
public record CaixaCriadaResponse(string CodigoCaixa, string Produto);

public static class RecebimentoEndpoints
{
    public static void MapRecebimentos(this WebApplication app)
    {
        var grupo = app.MapGroup("/recebimentos").WithTags("Entrada (conferente)")
            .RequireAuthorization(p => p.RequireRole("Conferente"));

        // RF06: bipa o código de barras da nota e começa a entrada
        grupo.MapPost("/", async (IniciarEntradaRequest dados, EntradaService entrada, ClaimsPrincipal user) =>
        {
            var recebimento = await entrada.IniciarAsync(dados.ChaveNota, user.UsuarioId());
            return Results.Created($"/recebimentos/{recebimento.Id}", await entrada.DetalharAsync(recebimento.Id));
        });

        // RF07: conferência (nota x bipado)
        grupo.MapGet("/{id:int}", async (int id, EntradaService entrada) =>
            Results.Ok(await entrada.DetalharAsync(id)));

        // RF06: bipa o produto; o sistema cria a caixa e devolve o código da etiqueta (RN01)
        grupo.MapPost("/{id:int}/caixas", async (int id, BiparProdutoRequest dados, EntradaService entrada, ClaimsPrincipal user) =>
        {
            var caixa = await entrada.BiparAsync(id, dados.CodigoProduto, user.UsuarioId());
            return Results.Ok(new CaixaCriadaResponse(caixa.Codigo, caixa.Produto!.Descricao));
        });

        // RF07 / RN03: finaliza; com diferença, fica esperando o gerente no Web
        grupo.MapPost("/{id:int}/finalizar", async (int id, EntradaService entrada) =>
        {
            await entrada.FinalizarAsync(id);
            return Results.Ok(await entrada.DetalharAsync(id));
        });
    }
}
