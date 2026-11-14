# 12 — Triggers e procedures (≈ 25 min)

**Objetivo:** a **Etapa 7** do manual pede *procedures*, *triggers*, otimização e segurança no banco. Este passo cria 2 triggers e 2 procedures **por migration**, assim ficam versionados junto com o código.

| Objeto | Tipo | Para quê |
|---|---|---|
| `TR_Carregamentos_BloqueiaManutencao` | Trigger | **RN17** também no banco: mesmo um INSERT direto não usa veículo em manutenção |
| `TR_MovimentacoesCaixa_ImpedeExclusao` | Trigger | **Segurança**: o histórico das caixas não pode ser apagado |
| `sp_HistoricoCaixa` | Procedure | **RF19**: histórico de uma caixa pelo código |
| `sp_ResumoCarregamento` | Procedure | **RN11**: total de caixas × caixas bipadas |

## 1. Avisar o EF Core sobre os triggers

**Isso é obrigatório:** sem esse aviso, o EF dá erro ao inserir em tabela com trigger no SQL Server.

No `PimDbContext.cs`, dentro do `OnModelCreating`, **antes** do `foreach`:

```csharp
// Tabelas com trigger no banco (avisa o EF para não usar OUTPUT no INSERT)
mb.Entity<Carregamento>().ToTable(t => t.HasTrigger("TR_Carregamentos_BloqueiaManutencao"));
mb.Entity<MovimentacaoCaixa>().ToTable(t => t.HasTrigger("TR_MovimentacoesCaixa_ImpedeExclusao"));
```

## 2. Migration

```fish
dotnet ef migrations add TriggersEProcedures --project Pim.Core --startup-project Pim.Web
```

Abra o arquivo gerado em `Pim.Core/Migrations/<data>_TriggersEProcedures.cs` e substitua o conteúdo:

**`src/Pim.Core/Migrations/<data>_TriggersEProcedures.cs`**

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pim.Core.Migrations
{
    /// <inheritdoc />
    public partial class TriggersEProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TRIGGER 1 (RN17): o banco também impede carregamento com veículo em manutenção
            // Situacao = 2 é SituacaoVeiculo.EmManutencao
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_Carregamentos_BloqueiaManutencao
ON Carregamentos
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Veiculos v ON v.Id = i.VeiculoId
               WHERE v.Situacao = 2)
        THROW 50001, 'RN17: veiculo em manutencao nao pode ser usado em carregamento.', 1;
END");

            // TRIGGER 2 (segurança): o histórico das caixas não pode ser apagado
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_MovimentacoesCaixa_ImpedeExclusao
ON MovimentacoesCaixa
INSTEAD OF DELETE
AS
BEGIN
    THROW 50002, 'O historico de movimentacao das caixas nao pode ser apagado.', 1;
END");

            // PROCEDURE 1 (RF19): histórico de uma caixa pelo código
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_HistoricoCaixa
    @Codigo NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Codigo, p.Descricao AS Produto, m.Tipo, m.DataHora,
           u.Nome AS Usuario, m.CarregamentoId
    FROM Caixas c
    JOIN Produtos p ON p.Id = c.ProdutoId
    JOIN MovimentacoesCaixa m ON m.CaixaId = c.Id
    JOIN Usuarios u ON u.Id = m.UsuarioId
    WHERE c.Codigo = @Codigo
    ORDER BY m.DataHora;
END");

            // PROCEDURE 2 (RN11): quantas caixas o carregamento tem e quantas já foram bipadas
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_ResumoCarregamento
    @CarregamentoId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT ISNULL(SUM(i.Quantidade), 0)
         FROM CarregamentoPedidos cp
         JOIN PedidoVendaItens i ON i.PedidoVendaId = cp.PedidoVendaId
         WHERE cp.CarregamentoId = @CarregamentoId) AS TotalCaixas,
        (SELECT COUNT(*)
         FROM Caixas
         WHERE CarregamentoId = @CarregamentoId) AS CaixasBipadas;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_ResumoCarregamento");
            migrationBuilder.Sql("DROP PROCEDURE sp_HistoricoCaixa");
            migrationBuilder.Sql("DROP TRIGGER TR_MovimentacoesCaixa_ImpedeExclusao");
            migrationBuilder.Sql("DROP TRIGGER TR_Carregamentos_BloqueiaManutencao");
        }
    }
}
```

> No SQL Server, os enums são guardados como número: `Situacao = 2` é `EmManutencao`, porque a contagem começa em 0.

Aplique a migration:

```fish
dotnet ef database update --project Pim.Core --startup-project Pim.Web
```

## 3. Testar

Para não repetir o comando inteiro, crie um atalho no fish:

```fish
alias sql "docker exec -it pim-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Pim@2026forte' -C -d PimDb -W -Q"
```

| Comando | Resultado esperado |
|---|---|
| `sql "EXEC sp_HistoricoCaixa 'CX00000001'"` | 1 linha: Produto A, entrada pela Juliana |
| `sql "EXEC sp_ResumoCarregamento 1"` | Total de caixas e caixas bipadas do carregamento 1 |
| `sql "DELETE FROM MovimentacoesCaixa WHERE Id = 1"` | **Erro 50002**: o histórico não pode ser apagado |
| `sql "INSERT INTO Carregamentos (VeiculoId, MotoristaId, DataPrevista, Status) VALUES ((SELECT TOP 1 Id FROM Veiculos WHERE Situacao = 2), (SELECT TOP 1 Id FROM Motoristas), GETDATE(), 0)"` | **Erro 50001**: RN17 |

E pelo site: criar um carregamento válido **continua funcionando**, o que mostra que o `HasTrigger` está certo.

## Para o texto do PIM (Etapa 7)
- **Otimização:** índices únicos em `Login`, `Placa`, `Caixa.Codigo`, `NotaFiscal.Chave`, `Cpf` e `PedidoVenda.Numero`. Esses campos são os mais buscados (todo bip procura a caixa pelo código), e o EF cria índice em todas as chaves estrangeiras.
- **Segurança:** senhas com hash BCrypt, histórico protegido por trigger, sem exclusão em cascata e CPF mascarado na tela (LGPD).
