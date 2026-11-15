using GatumsGourmet.Domain.Entities.Estoque;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Estoque;

public class MovimentoEstoqueConfiguration : IEntityTypeConfiguration<MovimentoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentoEstoque> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Tipo).HasConversion<string>().IsRequired();
        b.Property(x => x.Quantidade).IsRequired();
        b.Property(x => x.SaldoResultante).IsRequired();
        b.Property(x => x.Observacao).HasMaxLength(500);
        b.HasIndex(x => x.ProdutoId);
        b.HasIndex(x => x.Data);
    }
}
