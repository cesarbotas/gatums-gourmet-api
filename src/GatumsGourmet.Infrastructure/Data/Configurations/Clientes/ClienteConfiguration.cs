using GatumsGourmet.Domain.Entities.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Clientes;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.Property(x => x.Telefone).HasMaxLength(20);
        b.Property(x => x.CriadoEm).IsRequired();

        b.HasOne(x => x.Endereco)
            .WithOne("ClienteId")
            .HasForeignKey<Endereco>(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
