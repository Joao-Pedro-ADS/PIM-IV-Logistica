# 04 — Dados de teste / simulação do ERP (≈ 25 min)

**Objetivo (RF04):** o ERP é **simulado**. Esta classe coloca no banco o que viria dele (clientes, produtos, notas, pedidos) e alguns cadastros para teste.

## 1. Classe de dados iniciais

Crie `src/Pim.Core/Dados/DadosIniciais.cs`:

**`src/Pim.Core/Dados/DadosIniciais.cs`**

```csharp
using Pim.Core.Entidades;

namespace Pim.Core.Dados;

// Simula os dados que viriam do ERP (RF04) + cadastros para teste
public static class DadosIniciais
{
    public static void Popular(PimDbContext db)
    {
        if (db.Usuarios.Any()) return;   // já populado

        // ---------- Usuários (senha de todos: 123456) ----------
        var senha = BCrypt.Net.BCrypt.HashPassword("123456");
        var gerente = new Usuario { Nome = "Roberto Almeida", Login = "gerente", SenhaHash = senha, Perfil = Perfil.Gerente };
        var conferente = new Usuario { Nome = "Juliana Souza", Login = "conferente", SenhaHash = senha, Perfil = Perfil.Conferente };
        var usuarioMotorista = new Usuario { Nome = "Marcos Silva", Login = "motorista", SenhaHash = senha, Perfil = Perfil.Motorista };
        db.Usuarios.AddRange(
            gerente, conferente, usuarioMotorista,
            new Usuario { Nome = "Célia Ramos", Login = "programador", SenhaHash = senha, Perfil = Perfil.Programador },
            new Usuario { Nome = "Carlos Lima", Login = "frota", SenhaHash = senha, Perfil = Perfil.Frota },
            new Usuario { Nome = "Paulo Mendes", Login = "comercial", SenhaHash = senha, Perfil = Perfil.Comercial },
            new Usuario { Nome = "Diego Rocha", Login = "operador", SenhaHash = senha, Perfil = Perfil.Operador },
            new Usuario { Nome = "Antônio Pereira", Login = "porteiro", SenhaHash = senha, Perfil = Perfil.Porteiro });

        // ---------- Fornecedores e produtos ----------
        var alfa = new Fornecedor { Nome = "Fornecedor Alfa Ltda", Cnpj = "11222333000181" };
        var beta = new Fornecedor { Nome = "Beta Distribuição S.A.", Cnpj = "44555666000172" };
        db.Fornecedores.AddRange(alfa, beta);

        var prodA = new Produto { Codigo = "7891000100103", Descricao = "Produto A" };
        var prodB = new Produto { Codigo = "7891000200206", Descricao = "Produto B" };
        var prodC = new Produto { Codigo = "7891000300309", Descricao = "Produto C" };
        var prodD = new Produto { Codigo = "7891000400402", Descricao = "Produto D" };
        db.Produtos.AddRange(prodA, prodB, prodC, prodD);

        // ---------- Clientes e endereços ----------
        var mercado = new Cliente
        {
            Nome = "Mercado Bom Preço", CpfCnpj = "12345678000190", Telefone = "(11) 4000-1000",
            Enderecos =
            {
                new Endereco { Logradouro = "Rua das Flores", Numero = "100", Bairro = "Centro", Cidade = "São Paulo", Uf = "SP", Cep = "01001000" },
                new Endereco { Logradouro = "Av. Brasil", Numero = "2500", Bairro = "Jardim América", Cidade = "São Paulo", Uf = "SP", Cep = "01430001" }
            }
        };
        var padaria = new Cliente
        {
            Nome = "Padaria Pão Quente", CpfCnpj = "98765432000110", Telefone = "(11) 4000-2000",
            Enderecos = { new Endereco { Logradouro = "Rua XV de Novembro", Numero = "45", Bairro = "Vila Nova", Cidade = "Guarulhos", Uf = "SP", Cep = "07010000" } }
        };
        var restaurante = new Cliente
        {
            Nome = "Restaurante Sabor Caseiro", CpfCnpj = "55443322000199", Telefone = "(11) 4000-3000",
            Enderecos = { new Endereco { Logradouro = "Rua do Comércio", Numero = "800", Bairro = "Centro", Cidade = "Osasco", Uf = "SP", Cep = "06010000" } }
        };
        db.Clientes.AddRange(mercado, padaria, restaurante);

        // ---------- Pedidos de compra e notas fiscais de entrada ----------
        var pc1 = new PedidoCompra
        {
            Numero = 1001, Fornecedor = alfa, DataEmissao = DateTime.Today.AddDays(-5),
            Itens = { new PedidoCompraItem { Produto = prodA, Quantidade = 20 }, new PedidoCompraItem { Produto = prodB, Quantidade = 10 } }
        };
        var pc2 = new PedidoCompra
        {
            Numero = 1002, Fornecedor = beta, DataEmissao = DateTime.Today.AddDays(-3),
            Itens = { new PedidoCompraItem { Produto = prodC, Quantidade = 6 }, new PedidoCompraItem { Produto = prodD, Quantidade = 4 } }
        };
        db.PedidosCompra.AddRange(pc1, pc2);

        var nf1 = new NotaFiscal
        {
            Numero = 4521, Chave = "35261111222333000181550010000045211000045210",
            Fornecedor = alfa, PedidoCompra = pc1, DataEmissao = DateTime.Today.AddDays(-2), QuantidadeVolumes = 30,
            Itens = { new NotaFiscalItem { Produto = prodA, Quantidade = 20 }, new NotaFiscalItem { Produto = prodB, Quantidade = 10 } }
        };
        var nf2 = new NotaFiscal
        {
            Numero = 7788, Chave = "35261144555666000172550010000077881000077880",
            Fornecedor = beta, PedidoCompra = pc2, DataEmissao = DateTime.Today.AddDays(-1), QuantidadeVolumes = 10,
            Itens = { new NotaFiscalItem { Produto = prodC, Quantidade = 6 }, new NotaFiscalItem { Produto = prodD, Quantidade = 4 } }
        };
        var nf3 = new NotaFiscal   // nota ainda não recebida: para testar o app
        {
            Numero = 9010, Chave = "35261111222333000181550010000090101000090100",
            Fornecedor = alfa, DataEmissao = DateTime.Today, QuantidadeVolumes = 5,
            Itens = { new NotaFiscalItem { Produto = prodB, Quantidade = 5 } }
        };
        db.NotasFiscais.AddRange(nf1, nf2, nf3);

        // ---------- Recebimentos (caixas já bipadas) ----------
        int sequencia = 1;
        List<Caixa> GerarCaixas(Produto produto, int quantidade, DateTime quando)
        {
            var caixas = new List<Caixa>();
            for (int i = 0; i < quantidade; i++)
            {
                caixas.Add(new Caixa
                {
                    Codigo = $"CX{sequencia++:D8}",
                    Produto = produto,
                    Movimentacoes = { new MovimentacaoCaixa { Tipo = TipoMovimentacao.Entrada, DataHora = quando, Usuario = conferente } }
                });
            }
            return caixas;
        }

        var ontem = DateTime.Today.AddDays(-1).AddHours(8);
        var rec1 = new Recebimento   // bateu com a nota
        {
            NotaFiscal = nf1, Conferente = conferente, DataInicio = ontem, DataFim = ontem.AddMinutes(40),
            Status = StatusRecebimento.Finalizado
        };
        rec1.Caixas.AddRange(GerarCaixas(prodA, 20, ontem.AddMinutes(10)));
        rec1.Caixas.AddRange(GerarCaixas(prodB, 10, ontem.AddMinutes(30)));

        var hoje = DateTime.Today.AddHours(9);
        var rec2 = new Recebimento   // faltaram 2 caixas do Produto D: aguarda o gerente (RN03)
        {
            NotaFiscal = nf2, Conferente = conferente, DataInicio = hoje, Status = StatusRecebimento.AguardandoAprovacao
        };
        rec2.Caixas.AddRange(GerarCaixas(prodC, 6, hoje.AddMinutes(5)));
        rec2.Caixas.AddRange(GerarCaixas(prodD, 2, hoje.AddMinutes(10)));
        db.Recebimentos.AddRange(rec1, rec2);

        // ---------- Pedidos de venda (vindos do ERP) ----------
        db.PedidosVenda.AddRange(
            new PedidoVenda
            {
                Numero = 5001, Cliente = mercado, Endereco = mercado.Enderecos[0], DataEmissao = DateTime.Today.AddDays(-2),
                Itens = { new PedidoVendaItem { Produto = prodA, Quantidade = 8 }, new PedidoVendaItem { Produto = prodB, Quantidade = 4 } }
            },
            new PedidoVenda
            {
                Numero = 5002, Cliente = padaria, Endereco = padaria.Enderecos[0], DataEmissao = DateTime.Today.AddDays(-2),
                Itens = { new PedidoVendaItem { Produto = prodA, Quantidade = 5 }, new PedidoVendaItem { Produto = prodC, Quantidade = 3 } }
            },
            new PedidoVenda
            {
                Numero = 5003, Cliente = restaurante, Endereco = restaurante.Enderecos[0], DataEmissao = DateTime.Today.AddDays(-1),
                Itens = { new PedidoVendaItem { Produto = prodB, Quantidade = 6 } }
            },
            new PedidoVenda
            {
                Numero = 5004, Cliente = mercado, Endereco = mercado.Enderecos[1], DataEmissao = DateTime.Today.AddDays(-1),
                Itens = { new PedidoVendaItem { Produto = prodC, Quantidade = 2 }, new PedidoVendaItem { Produto = prodD, Quantidade = 2 } }
            },
            new PedidoVenda
            {
                Numero = 5005, Cliente = padaria, Endereco = padaria.Enderecos[0], DataEmissao = DateTime.Today,
                Itens = { new PedidoVendaItem { Produto = prodA, Quantidade = 7 } }
            });

        // ---------- Frota ----------
        db.Veiculos.AddRange(
            new Veiculo { Placa = "ABC1D23", Modelo = "Van Sprinter", LimiteCaixas = 15 },
            new Veiculo { Placa = "DEF4G56", Modelo = "Caminhão Toco", LimiteCaixas = 120 },
            new Veiculo { Placa = "GHI7J89", Modelo = "Caminhão Truck", LimiteCaixas = 250, Situacao = SituacaoVeiculo.EmManutencao });

        db.Motoristas.AddRange(
            new Motorista { Nome = "Marcos Silva", Cpf = "12345678901", NumeroCnh = "01234567890", ValidadeCnh = DateTime.Today.AddYears(2), Usuario = usuarioMotorista },
            new Motorista { Nome = "João Ferreira", Cpf = "23456789012", NumeroCnh = "02345678901", ValidadeCnh = DateTime.Today.AddDays(20) },
            new Motorista { Nome = "Pedro Costa", Cpf = "34567890123", NumeroCnh = "03456789012", ValidadeCnh = DateTime.Today.AddDays(-10) });

        db.SaveChanges();
    }
}
```

### O que tem nos dados (pensado para testar as regras)

| Dado | Para testar |
|---|---|
| Nota **4521**: 30 caixas, todas bipadas | Entrada que bateu com a nota |
| Nota **7788**: faltam 2 caixas do Produto D | **RN03**: o gerente precisa aprovar |
| Nota **9010**: ainda não recebida | Testar a entrada pelo app depois |
| Van **ABC1D23** com limite de 15 caixas | **RN09**: escolher pedidos que somam mais de 15 |
| Truck **GHI7J89** em manutenção | **RN17** |
| Motorista **Pedro Costa** com CNH vencida | **RN18** |
| Motorista **João Ferreira** com CNH vencendo em 20 dias | Aviso de CNH vencendo |
| 5 pedidos de venda abertos | Montar carregamentos |

## 2. Rodar ao iniciar a aplicação

Em `src/Pim.Web/Program.cs`, logo **depois** de `var app = builder.Build();`:

```csharp
// Cria/atualiza o banco e coloca os dados de teste
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PimDbContext>();
    db.Database.Migrate();
    DadosIniciais.Popular(db);
}
```

`Migrate()` aplica as migrations sozinho, então daqui para frente nem precisa rodar o `database update`. Já o `Popular` só insere os dados se o banco estiver vazio.

## 3. Testar

```fish
dotnet run --project Pim.Web
```

Depois confira no banco:

```fish
docker exec -it pim-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Pim@2026forte' -C -d PimDb -Q "SELECT Numero, Status FROM PedidosVenda; SELECT COUNT(*) AS Caixas FROM Caixas"
```

Devem aparecer 5 pedidos com `Status = 0` (Aberto) e **38** caixas.

> **Para recomeçar do zero** (apagar tudo e recriar os dados):
> `dotnet ef database drop -f --project Pim.Core --startup-project Pim.Web` e rode a aplicação de novo.

## Pronto quando
- [ ] a aplicação sobe sem erro
- [ ] os pedidos e as caixas aparecem no banco
