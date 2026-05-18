using GatumsGourmet.Domain.Entities.Carrinho;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Carrinho;

public class CarrinhoItemConfiguration : IEntityTypeConfiguration<CarrinhoItem>
{
    public void Configure(EntityTypeBuilder<CarrinhoItem> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UsuarioId, x.ProdutoId });
        b.Property(x => x.Preco).HasColumnType("numeric(18,2)");
    }
}
