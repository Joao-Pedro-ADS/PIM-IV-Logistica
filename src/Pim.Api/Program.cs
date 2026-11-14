using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Pim.Api.Endpoints;
using Pim.Core.Dados;
using Pim.Core.Servicos;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados (o mesmo do Web)
builder.Services.AddDbContext<PimDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Pim")));

// Serviços do Pim.Core: as mesmas regras do Web
builder.Services.AddScoped<AcessoService>();
builder.Services.AddScoped<RecebimentoService>();
builder.Services.AddScoped<EntradaService>();
builder.Services.AddScoped<ExpedicaoService>();
builder.Services.AddScoped<PortariaService>();
builder.Services.AddScoped<EntregaService>();
builder.Services.AddScoped<RetornoService>();

// Login com token JWT (o app guarda o token e manda em cada requisição)
var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Chave"]!));
builder.Services.AddSingleton(chave);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Emissor"],
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = chave,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

// Cria/atualiza o banco e coloca os dados de teste (igual ao Web)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PimDbContext>();
    db.Database.Migrate();
    DadosIniciais.Popular(db);
}

// Regra de negócio violada vira 400 com a mensagem da regra
app.Use(async (contexto, proximo) =>
{
    try
    {
        await proximo();
    }
    catch (RegraNegocioException e)
    {
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        await contexto.Response.WriteAsJsonAsync(new { erro = e.Message });
    }
});

app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Pim.Api v1"));

app.UseAuthentication();
app.UseAuthorization();

// Cada grupo de rotas fica no seu arquivo em Endpoints/
app.MapAuth();
app.MapRecebimentos();
app.MapCarregamentos();
app.MapPortaria();
app.MapEntregas();
app.MapRetornos();

app.Run();
