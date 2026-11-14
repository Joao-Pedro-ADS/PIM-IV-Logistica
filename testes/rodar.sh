#!/usr/bin/env bash
# Roda um conjunto de testes contra uma base de teste zerada (PimDbTeste).
# Uso: ./rodar.sh web   -> testar.py      (Web, 36 verificações)
#      ./rodar.sh api   -> testar_api.py  (ciclo do app pela API)
#      ./rodar.sh latencia -> medir_latencia.py (p50/p95/p99 da bipagem)
# Requer o contêiner pim-sql (SQL Server) e o .NET 10. Não toca no banco PimDb.
set -u
cd "$(dirname "$0")"
DB=PimDbTeste
SENHA='Pim@2026forte'
SQL=(docker exec pim-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SENHA")

case "${1:-}" in
  web) SCRIPT=testar.py ;;
  api) SCRIPT=testar_api.py ;;
  latencia) SCRIPT=medir_latencia.py ;;
  *) echo "uso: $0 web|api|latencia"; exit 2 ;;
esac

docker start pim-sql >/dev/null
for _ in $(seq 1 30); do "${SQL[@]}" -Q "SELECT 1" >/dev/null 2>&1 && break; sleep 1; done
"${SQL[@]}" -Q "IF DB_ID('$DB') IS NOT NULL BEGIN ALTER DATABASE $DB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE $DB; END" >/dev/null

export ConnectionStrings__Pim="Server=localhost,1433;Database=$DB;User Id=sa;Password=$SENHA;TrustServerCertificate=True"
dotnet build ../src/Pim.slnx -v q >/dev/null || exit 1

esperar() { for _ in $(seq 1 60); do curl -s -o /dev/null "$1" && return 0; sleep 1; done; echo "nao subiu: $1"; return 1; }

# o Web sobe primeiro: ele cria o banco e os dados de teste
dotnet run --no-build --project ../src/Pim.Web --no-launch-profile --urls http://localhost:5080 >/tmp/pim-web.log 2>&1 &
WEB=$!
esperar http://localhost:5080/Conta/Login
ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build --project ../src/Pim.Api --no-launch-profile --urls http://localhost:5090 >/tmp/pim-api.log 2>&1 &
API=$!
esperar http://localhost:5090/openapi/v1.json

PIM_DB=$DB python3 "$SCRIPT"
RESULTADO=$?

kill $WEB $API 2>/dev/null
pkill -f 'Pim[.]Web|Pim[.]Api' 2>/dev/null
exit $RESULTADO
