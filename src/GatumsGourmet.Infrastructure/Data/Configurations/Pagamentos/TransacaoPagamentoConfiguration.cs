using GatumsGourmet.Domain.Entities.Pagamentos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Pagamentos;

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
