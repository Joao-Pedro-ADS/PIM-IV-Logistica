using System.Security.Claims;
using Pim.Core.Servicos;

namespace Pim.Api.Endpoints;

public record ChegadaFornecedorRequest(string Placa);
public record QrVeiculoRequest(string Qr);

public static class PortariaEndpoints
{
    public static void MapPortaria(this WebApplication app)
    {
        var grupo = app.MapGroup("/portaria").WithTags("Portaria (porteiro)")
            .RequireAuthorization(p => p.RequireRole("Porteiro"));

        // RF05
        grupo.MapPost("/chegada-fornecedor", async (ChegadaFornecedorRequest dados, PortariaService portaria, ClaimsPrincipal user) =>
        {
            var registro = await portaria.ChegadaFornecedorAsync(dados.Placa, user.UsuarioId());
            return Results.Ok(new { registro.Id, registro.PlacaFornecedor, registro.DataHora });
        });

        // RF12 / RN11: só sai com todas as caixas bipadas
        grupo.MapPost("/saida", async (QrVeiculoRequest dados, PortariaService portaria, ClaimsPrincipal user) =>
            Results.Ok(await portaria.SaidaAsync(dados.Qr, user.UsuarioId())));

        // RF14
        grupo.MapPost("/volta", async (QrVeiculoRequest dados, PortariaService portaria, ClaimsPrincipal user) =>
            Results.Ok(await portaria.VoltaAsync(dados.Qr, user.UsuarioId())));
    }
}
