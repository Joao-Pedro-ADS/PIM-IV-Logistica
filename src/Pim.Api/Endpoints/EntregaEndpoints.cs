using System.Security.Claims;
using Pim.Core.Servicos;

namespace Pim.Api.Endpoints;

public record RegistrarEntregaRequest(bool Entregue, string? Motivo);

public static class EntregaEndpoints
{
    public static void MapEntregas(this WebApplication app)
    {
        var grupo = app.MapGroup("/entregas").WithTags("Entrega (motorista)")
            .RequireAuthorization(p => p.RequireRole("Motorista"));

        // RF13: pedidos do carregamento em rota do motorista logado
        grupo.MapGet("/", async (EntregaService entregas, ClaimsPrincipal user) =>
            Results.Ok(await entregas.MinhasEntregasAsync(user.UsuarioId())));

        // RF13 / RN13 (e RF16: encerra o carregamento quando é o caso)
        grupo.MapPost("/{id:int}", async (int id, RegistrarEntregaRequest dados, EntregaService entregas, ClaimsPrincipal user) =>
        {
            await entregas.RegistrarAsync(id, dados.Entregue, dados.Motivo, user.UsuarioId());
            return Results.Ok(await entregas.MinhasEntregasAsync(user.UsuarioId()));
        });
    }
}
