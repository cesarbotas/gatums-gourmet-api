using GatumsGourmet.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Auth;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Nome).IsUnique();
        b.Property(x => x.Descricao).HasMaxLength(300);
        b.Property(x => x.Sistema).HasDefaultValue(false);
    }
}
