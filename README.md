# PIM IV — Sistema de rastreabilidade para distribuidora

Projeto Integrado Multidisciplinar IV (Análise e Desenvolvimento de Sistemas — UNIP, 2026).
Rastreia caixas e veículos de uma distribuidora fictícia: entrada das mercadorias, carregamento,
saída pela portaria, entrega e retorno.

| Pasta | Conteúdo |
|---|---|
| `src/Pim.Core` | Entidades, serviços de regra de negócio e acesso a dados (EF Core + SQL Server) |
| `src/Pim.Web` | Aplicação web ASP.NET Core MVC (gerente, programador de cargas, frota, comercial) |
| `src/Pim.Api` | Minimal API REST com autenticação JWT, consumida pelo aplicativo |
| `app` | Aplicativo Flutter (conferente, operador, porteiro, motorista) |
| `testes` | Testes de ponta a ponta do Web e da API e medição de latência |
| `relatorio` | Relatório em LaTeX (`0_main.pdf`) e diagramas |
| `docs` | Contexto do negócio, requisitos, regras e passo a passo |

## Como executar

Requisitos: .NET 10, Docker e Flutter.

```bash
# banco de dados
docker run -d --name pim-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Pim@2026forte' \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# web (cria o banco e os dados de teste na primeira execução)
dotnet run --project src/Pim.Web

# API, em outro terminal
dotnet run --project src/Pim.Api

# aplicativo
cd app && flutter run
```

Usuários de teste (senha `123456`): `gerente`, `programador`, `frota`, `comercial` no Web;
`conferente`, `operador`, `porteiro`, `motorista` no aplicativo.

A senha do banco e a chave do JWT em `appsettings.json` são apenas de desenvolvimento.

## Testes

```bash
./testes/rodar.sh web       # 36 verificações no Web
./testes/rodar.sh api       # 48 verificações no ciclo do app pela API
./testes/rodar.sh latencia  # p50/p95/p99 da bipagem
```

Cada execução recria uma base de teste separada (`PimDbTeste`).
