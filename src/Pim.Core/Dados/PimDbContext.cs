using Microsoft.EntityFrameworkCore;
using Pim.Core.Entidades;

namespace Pim.Core.Dados;

public class PimDbContext : DbContext
{
    public PimDbContext(DbContextOptions<PimDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Endereco> Enderecos => Set<Endereco>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<PedidoCompra> PedidosCompra => Set<PedidoCompra>();
    public DbSet<PedidoCompraItem> PedidoCompraItens => Set<PedidoCompraItem>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<NotaFiscalItem> NotaFiscalItens => Set<NotaFiscalItem>();
    public DbSet<Recebimento> Recebimentos => Set<Recebimento>();
    public DbSet<Caixa> Caixas => Set<Caixa>();
    public DbSet<MovimentacaoCaixa> MovimentacoesCaixa => Set<MovimentacaoCaixa>();
    public DbSet<PedidoVenda> PedidosVenda => Set<PedidoVenda>();
    public DbSet<PedidoVendaItem> PedidoVendaItens => Set<PedidoVendaItem>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Motorista> Motoristas => Set<Motorista>();
    public DbSet<Carregamento> Carregamentos => Set<Carregamento>();
    public DbSet<CarregamentoPedido> CarregamentoPedidos => Set<CarregamentoPedido>();
    public DbSet<RegistroPortaria> RegistrosPortaria => Set<RegistroPortaria>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Campos únicos (índice) — texto indexado precisa de tamanho máximo
        mb.Entity<Usuario>().Property(u => u.Login).HasMaxLength(30);
        mb.Entity<Usuario>().HasIndex(u => u.Login).IsUnique();

        mb.Entity<Cliente>().Property(c => c.CpfCnpj).HasMaxLength(14);
        mb.Entity<Cliente>().HasIndex(c => c.CpfCnpj).IsUnique();

        mb.Entity<Produto>().Property(p => p.Codigo).HasMaxLength(14);
        mb.Entity<Produto>().HasIndex(p => p.Codigo).IsUnique();

        mb.Entity<NotaFiscal>().Property(n => n.Chave).HasMaxLength(44);
        mb.Entity<NotaFiscal>().HasIndex(n => n.Chave).IsUnique();

        mb.Entity<Caixa>().Property(c => c.Codigo).HasMaxLength(20);
        mb.Entity<Caixa>().HasIndex(c => c.Codigo).IsUnique();          // RN01

        mb.Entity<Veiculo>().HasIndex(v => v.Placa).IsUnique();
        mb.Entity<Motorista>().HasIndex(m => m.Cpf).IsUnique();
        mb.Entity<PedidoVenda>().HasIndex(p => p.Numero).IsUnique();

        // Recebimento tem duas ligações com Usuario
        mb.Entity<Recebimento>().HasOne(r => r.Conferente).WithMany().HasForeignKey(r => r.ConferenteId);
        mb.Entity<Recebimento>().HasOne(r => r.AprovadoPor).WithMany().HasForeignKey(r => r.AprovadoPorId);

        // Tabelas com trigger no banco (avisa o EF para não usar OUTPUT no INSERT)
        mb.Entity<Carregamento>().ToTable(t => t.HasTrigger("TR_Carregamentos_BloqueiaManutencao"));
        mb.Entity<MovimentacaoCaixa>().ToTable(t => t.HasTrigger("TR_MovimentacoesCaixa_ImpedeExclusao"));

        // Nada é apagado em cascata: o histórico precisa ser mantido
        foreach (var fk in mb.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
