using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Pim.Core.Dados;
using Pim.Core.Servicos;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados
builder.Services.AddDbContext<PimDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Pim")));
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<CarregamentoService>();
builder.Services.AddScoped<RecebimentoService>();

// Login com cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Conta/Login";
        o.AccessDeniedPath = "/Conta/AcessoNegado";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AuthorizeFilter());                        // toda página exige login
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());  // proteção contra CSRF nos formulários
});

var app = builder.Build();

// Cria/atualiza o banco e coloca os dados de teste
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PimDbContext>();
    db.Database.Migrate();
    DadosIniciais.Popular(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
