using GatumsGourmet.Domain.Entities.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Produtos;

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
