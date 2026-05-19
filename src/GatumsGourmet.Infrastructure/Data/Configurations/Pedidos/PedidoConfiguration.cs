using GatumsGourmet.Domain.Entities.Pedidos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Pedidos;

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
