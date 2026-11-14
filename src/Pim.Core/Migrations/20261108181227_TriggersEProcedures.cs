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
