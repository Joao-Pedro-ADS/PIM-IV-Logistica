using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pim.Core.Entidades;
using Pim.Core.Servicos;

namespace Pim.Api.Endpoints;

public record LoginRequest(string Login, string Senha);
public record LoginResponse(string Token, string Nome, string Perfil, DateTime ExpiraEm);

public static class AuthEndpoints
{
    // Perfis que usam o app; os outros entram pelo Web
    private static readonly Perfil[] PerfisApp = { Perfil.Conferente, Perfil.Operador, Perfil.Porteiro, Perfil.Motorista };

    public static void MapAuth(this WebApplication app)
    {
        var grupo = app.MapGroup("/auth").WithTags("Login");

        // RF01
        grupo.MapPost("/login", async (LoginRequest dados, AcessoService acesso, SymmetricSecurityKey chave, IConfiguration config) =>
        {
            var usuario = await acesso.AutenticarAsync(dados.Login ?? "", dados.Senha ?? "");
            if (usuario == null)
                return Results.BadRequest(new { erro = "Login ou senha inválidos." });
            if (!PerfisApp.Contains(usuario.Perfil))
                return Results.BadRequest(new { erro = "Este perfil usa o sistema Web." });

            var expira = DateTime.UtcNow.AddHours(config.GetValue("Jwt:HorasValidade", 12));
            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nome),
                    new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
                }),
                Issuer = config["Jwt:Emissor"],
                Expires = expira,
                SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)
            });

            return Results.Ok(new LoginResponse(token, usuario.Nome, usuario.Perfil.ToString(), expira));
        });
    }

    // Id do usuário logado, tirado do token
    public static int UsuarioId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
