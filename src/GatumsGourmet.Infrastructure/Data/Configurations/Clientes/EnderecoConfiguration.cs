using GatumsGourmet.Domain.Entities.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GatumsGourmet.Infrastructure.Data.Configurations.Clientes;

public class EnderecoConfiguration : IEntityTypeConfiguration<Endereco>
{
    public void Configure(EntityTypeBuilder<Endereco> b)
    {
        b.ToTable("Enderecos");

        b.HasKey(x => x.Id);

        b.Property(x => x.Logradouro)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Numero)
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.Complemento)
            .HasMaxLength(100);

        b.Property(x => x.Bairro)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.Cidade)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.Estado)
            .HasMaxLength(2)
            .IsRequired();

        b.Property(x => x.Cep)
            .HasMaxLength(8)
            .IsRequired();
    }
}
