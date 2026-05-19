using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Entities.Carrinho;
using GatumsGourmet.Domain.Entities.Clientes;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Entities.Vendas;
using GatumsGourmet.Infrastructure.Data.Configurations.Auth;
using GatumsGourmet.Infrastructure.Data.Configurations.Carrinho;
using GatumsGourmet.Infrastructure.Data.Configurations.Clientes;
using GatumsGourmet.Infrastructure.Data.Configurations.Pagamentos;
using GatumsGourmet.Infrastructure.Data.Configurations.Pedidos;
using GatumsGourmet.Infrastructure.Data.Configurations.Produtos;
using GatumsGourmet.Infrastructure.Data.Configurations.Vendas;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data;

public class GatumsDbContext(DbContextOptions<GatumsDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<ProdutoComboItem> ProdutoComboItens => Set<ProdutoComboItem>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CarrinhoItem> CarrinhoItens => Set<CarrinhoItem>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();
    public DbSet<PedidoTimelineEvento> PedidoTimelineEventos => Set<PedidoTimelineEvento>();
    public DbSet<PedidoPagamento> PedidoPagamentos => Set<PedidoPagamento>();
    public DbSet<TransacaoPagamento> TransacoesPagamento => Set<TransacaoPagamento>();
    public DbSet<Venda> Vendas => Set<Venda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new ProdutoConfiguration());
        modelBuilder.ApplyConfiguration(new ProdutoComboItemConfiguration());
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new CarrinhoItemConfiguration());
        modelBuilder.ApplyConfiguration(new PedidoConfiguration());
        modelBuilder.ApplyConfiguration(new PedidoItemConfiguration());
        modelBuilder.ApplyConfiguration(new PedidoTimelineEventoConfiguration());
        modelBuilder.ApplyConfiguration(new PedidoPagamentoConfiguration());
        modelBuilder.ApplyConfiguration(new TransacaoPagamentoConfiguration());
        modelBuilder.ApplyConfiguration(new VendaConfiguration());
    }
}
