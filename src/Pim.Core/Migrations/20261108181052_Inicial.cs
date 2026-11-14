using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pim.Core.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CpfCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Login = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Perfil = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Veiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Placa = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LimiteCaixas = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enderecos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enderecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enderecos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidosCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    FornecedorId = table.Column<int>(type: "int", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosCompra_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Motoristas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    NumeroCnh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidadeCnh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motoristas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Motoristas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidosVenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    EnderecoId = table.Column<int>(type: "int", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_Enderecos_EnderecoId",
                        column: x => x.EnderecoId,
                        principalTable: "Enderecos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: false),
                    FornecedorId = table.Column<int>(type: "int", nullable: false),
                    PedidoCompraId = table.Column<int>(type: "int", nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantidadeVolumes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_PedidosCompra_PedidoCompraId",
                        column: x => x.PedidoCompraId,
                        principalTable: "PedidosCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoCompraItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoCompraId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoCompraItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoCompraItens_PedidosCompra_PedidoCompraId",
                        column: x => x.PedidoCompraId,
                        principalTable: "PedidosCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoCompraItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Carregamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VeiculoId = table.Column<int>(type: "int", nullable: false),
                    MotoristaId = table.Column<int>(type: "int", nullable: false),
                    DataPrevista = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataVolta = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carregamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carregamentos_Motoristas_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Motoristas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Carregamentos_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoVendaItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoVendaId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoVendaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoVendaItens_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoVendaItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotaFiscalItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotaFiscalId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaFiscalItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaFiscalItens_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotaFiscalItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recebimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotaFiscalId = table.Column<int>(type: "int", nullable: false),
                    ConferenteId = table.Column<int>(type: "int", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AprovadoPorId = table.Column<int>(type: "int", nullable: true),
                    Justificativa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recebimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recebimentos_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recebimentos_Usuarios_AprovadoPorId",
                        column: x => x.AprovadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recebimentos_Usuarios_ConferenteId",
                        column: x => x.ConferenteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarregamentoPedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarregamentoId = table.Column<int>(type: "int", nullable: false),
                    PedidoVendaId = table.Column<int>(type: "int", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataRegistro = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarregamentoPedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarregamentoPedidos_Carregamentos_CarregamentoId",
                        column: x => x.CarregamentoId,
                        principalTable: "Carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarregamentoPedidos_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosPortaria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VeiculoId = table.Column<int>(type: "int", nullable: true),
                    CarregamentoId = table.Column<int>(type: "int", nullable: true),
                    PlacaFornecedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PorteiroId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosPortaria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosPortaria_Carregamentos_CarregamentoId",
                        column: x => x.CarregamentoId,
                        principalTable: "Carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosPortaria_Usuarios_PorteiroId",
                        column: x => x.PorteiroId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosPortaria_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Caixas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    RecebimentoId = table.Column<int>(type: "int", nullable: false),
                    PedidoVendaId = table.Column<int>(type: "int", nullable: true),
                    CarregamentoId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Caixas_Carregamentos_CarregamentoId",
                        column: x => x.CarregamentoId,
                        principalTable: "Carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_Recebimentos_RecebimentoId",
                        column: x => x.RecebimentoId,
                        principalTable: "Recebimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimentacoesCaixa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaixaId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CarregamentoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentacoesCaixa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentacoesCaixa_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentacoesCaixa_Carregamentos_CarregamentoId",
                        column: x => x.CarregamentoId,
                        principalTable: "Carregamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentacoesCaixa_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_CarregamentoId",
                table: "Caixas",
                column: "CarregamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Codigo",
                table: "Caixas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_PedidoVendaId",
                table: "Caixas",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_ProdutoId",
                table: "Caixas",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_RecebimentoId",
                table: "Caixas",
                column: "RecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CarregamentoPedidos_CarregamentoId",
                table: "CarregamentoPedidos",
                column: "CarregamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CarregamentoPedidos_PedidoVendaId",
                table: "CarregamentoPedidos",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Carregamentos_MotoristaId",
                table: "Carregamentos",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_Carregamentos_VeiculoId",
                table: "Carregamentos",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_CpfCnpj",
                table: "Clientes",
                column: "CpfCnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enderecos_ClienteId",
                table: "Enderecos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Motoristas_Cpf",
                table: "Motoristas",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Motoristas_UsuarioId",
                table: "Motoristas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesCaixa_CaixaId",
                table: "MovimentacoesCaixa",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesCaixa_CarregamentoId",
                table: "MovimentacoesCaixa",
                column: "CarregamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesCaixa_UsuarioId",
                table: "MovimentacoesCaixa",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItens_NotaFiscalId",
                table: "NotaFiscalItens",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalItens_ProdutoId",
                table: "NotaFiscalItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_Chave",
                table: "NotasFiscais",
                column: "Chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_FornecedorId",
                table: "NotasFiscais",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_PedidoCompraId",
                table: "NotasFiscais",
                column: "PedidoCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoCompraItens_PedidoCompraId",
                table: "PedidoCompraItens",
                column: "PedidoCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoCompraItens_ProdutoId",
                table: "PedidoCompraItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosCompra_FornecedorId",
                table: "PedidosCompra",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_ClienteId",
                table: "PedidosVenda",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_EnderecoId",
                table: "PedidosVenda",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_Numero",
                table: "PedidosVenda",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVendaItens_PedidoVendaId",
                table: "PedidoVendaItens",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVendaItens_ProdutoId",
                table: "PedidoVendaItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Codigo",
                table: "Produtos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_AprovadoPorId",
                table: "Recebimentos",
                column: "AprovadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_ConferenteId",
                table: "Recebimentos",
                column: "ConferenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Recebimentos_NotaFiscalId",
                table: "Recebimentos",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPortaria_CarregamentoId",
                table: "RegistrosPortaria",
                column: "CarregamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPortaria_PorteiroId",
                table: "RegistrosPortaria",
                column: "PorteiroId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPortaria_VeiculoId",
                table: "RegistrosPortaria",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Login",
                table: "Usuarios",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_Placa",
                table: "Veiculos",
                column: "Placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarregamentoPedidos");

            migrationBuilder.DropTable(
                name: "MovimentacoesCaixa");

            migrationBuilder.DropTable(
                name: "NotaFiscalItens");

            migrationBuilder.DropTable(
                name: "PedidoCompraItens");

            migrationBuilder.DropTable(
                name: "PedidoVendaItens");

            migrationBuilder.DropTable(
                name: "RegistrosPortaria");

            migrationBuilder.DropTable(
                name: "Caixas");

            migrationBuilder.DropTable(
                name: "Carregamentos");

            migrationBuilder.DropTable(
                name: "PedidosVenda");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Recebimentos");

            migrationBuilder.DropTable(
                name: "Motoristas");

            migrationBuilder.DropTable(
                name: "Veiculos");

            migrationBuilder.DropTable(
                name: "Enderecos");

            migrationBuilder.DropTable(
                name: "NotasFiscais");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "PedidosCompra");

            migrationBuilder.DropTable(
                name: "Fornecedores");
        }
    }
}
