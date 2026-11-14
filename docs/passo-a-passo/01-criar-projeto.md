# 01 — Criar o projeto e o banco (≈ 30 min)

**Objetivo:** ter a solução .NET criada e um SQL Server rodando no Docker.

## 1. Ferramenta de migrations

```fish
dotnet tool install --global dotnet-ef
fish_add_path ~/.dotnet/tools
dotnet ef --version
```

## 2. Criar a solução

Na pasta do PIM:

```fish
cd ~/dev/studies/university/PIM04
mkdir src
cd src

dotnet new sln -n Pim
dotnet new classlib -n Pim.Core
dotnet new mvc -n Pim.Web
dotnet sln add Pim.Core Pim.Web
dotnet add Pim.Web reference Pim.Core
rm Pim.Core/Class1.cs
```

## 3. Pacotes

```fish
dotnet add Pim.Core package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Pim.Core package BCrypt.Net-Next
dotnet add Pim.Web package Microsoft.EntityFrameworkCore.Design
dotnet add Pim.Web package QRCoder
```

| Pacote | Para quê |
|---|---|
| EntityFrameworkCore.SqlServer | Acesso ao banco SQL Server |
| BCrypt.Net-Next | Guardar a senha criptografada (hash) |
| EntityFrameworkCore.Design | Comandos de migration |
| QRCoder | Gerar o QR Code dos veículos |

> Os avisos `NU1902/NU1903` sobre `System.Security.Cryptography.Pkcs` vêm de uma dependência do driver do SQL Server. Pode ignorar.

## 4. SQL Server no Docker

```fish
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Pim@2026forte" -p 1433:1433 --name pim-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Espere uns 20 segundos e confira com `docker ps`: o `pim-sql` deve aparecer como `Up`.
Nos próximos dias, se o computador reiniciar, basta rodar `docker start pim-sql`.

## 5. String de conexão

Em `src/Pim.Web/appsettings.json`, acrescente `ConnectionStrings` no começo:

**`src/Pim.Web/appsettings.json`**

```json
{
  "ConnectionStrings": {
    "Pim": "Server=localhost,1433;Database=PimDb;User Id=sa;Password=Pim@2026forte;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 6. Testar

```fish
dotnet build
dotnet run --project Pim.Web
```

Abra o endereço que aparece no terminal (ex.: `http://localhost:5xxx`). Deve aparecer a página padrão do ASP.NET.

## Pronto quando
- [ ] `dotnet build` termina com `0 Error(s)`
- [ ] `docker ps` mostra o `pim-sql` rodando
- [ ] a página padrão abre no navegador
