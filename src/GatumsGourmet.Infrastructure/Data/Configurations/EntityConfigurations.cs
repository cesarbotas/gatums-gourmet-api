using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.SenhaHash).IsRequired();
        b.Property(x => x.Role).HasConversion<string>();
    }
}

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        b.Property(x => x.Categoria).HasMaxLength(100).IsRequired();
        b.Property(x => x.Preco).HasColumnType("numeric(18,2)");
        b.Property(x => x.Tipo).HasConversion<string>();
        b.HasMany(x => x.ComboItens).WithOne().HasForeignKey(x => x.ComboProdutoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProdutoComboItemConfiguration : IEntityTypeConfiguration<ProdutoComboItem>
{
    public void Configure(EntityTypeBuilder<ProdutoComboItem> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
    }
}

public class CarrinhoItemConfiguration : IEntityTypeConfiguration<CarrinhoItem>
{
    public void Configure(EntityTypeBuilder<CarrinhoItem> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UsuarioId, x.ProdutoId });
        b.Property(x => x.Preco).HasColumnType("numeric(18,2)");
    }
}

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UsuarioId);
        b.Property(x => x.Total).HasColumnType("numeric(18,2)");
        b.Property(x => x.StatusAtual).HasConversion<string>();
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Timeline).WithOne().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Pagamento).WithOne().HasForeignKey<PedidoPagamento>(x => x.PedidoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PedidoItemConfiguration : IEntityTypeConfiguration<PedidoItem>
{
    public void Configure(EntityTypeBuilder<PedidoItem> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Preco).HasColumnType("numeric(18,2)");
    }
}

public class PedidoTimelineEventoConfiguration : IEntityTypeConfiguration<PedidoTimelineEvento>
{
    public void Configure(EntityTypeBuilder<PedidoTimelineEvento> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Etapa).HasConversion<string>();
        b.Property(x => x.Por).HasConversion<string>();
        b.Property(x => x.EntregaTipo).HasConversion<string>();
    }
}

public class PedidoPagamentoConfiguration : IEntityTypeConfiguration<PedidoPagamento>
{
    public void Configure(EntityTypeBuilder<PedidoPagamento> b)
    {
        b.HasKey(x => x.PedidoId);
    }
}

public class TransacaoPagamentoConfiguration : IEntityTypeConfiguration<TransacaoPagamento>
{
    public void Configure(EntityTypeBuilder<TransacaoPagamento> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.PedidoId);
        b.Property(x => x.Valor).HasColumnType("numeric(18,2)");
        b.Property(x => x.Metodo).HasConversion<string>();
        b.Property(x => x.Status).HasConversion<string>();
    }
}

public class VendaConfiguration : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Total).HasColumnType("numeric(18,2)");
    }
}
