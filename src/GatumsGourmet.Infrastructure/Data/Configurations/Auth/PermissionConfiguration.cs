using GatumsGourmet.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Auth;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Codigo).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.Property(x => x.Recurso).HasMaxLength(50).IsRequired();
        b.Property(x => x.Acao).HasMaxLength(50).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(300);
    }
}
