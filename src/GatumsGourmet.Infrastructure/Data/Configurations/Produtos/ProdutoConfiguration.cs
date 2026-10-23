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
        b.Property(x => x.Preco).HasColumnType("numeric(18,2)");
        b.Property(x => x.Tipo).HasConversion<string>();
        b.HasMany(x => x.ComboItens).WithOne().HasForeignKey(x => x.ComboProdutoId).OnDelete(DeleteBehavior.Cascade);

        // FK para Categoria. Restrict: não permite excluir categoria com produtos vinculados.
        b.HasOne(x => x.Categoria)
            .WithMany()
            .HasForeignKey(x => x.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CategoriaId);
    }
}

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();   // long autoincremento (identity)
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Nome).IsUnique();
        b.Property(x => x.Descricao).HasMaxLength(500);
        b.Property(x => x.Ativo).IsRequired();
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
