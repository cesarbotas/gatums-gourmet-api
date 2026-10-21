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

        // Endereco é uma owned entity do Cliente: suas colunas ficam na própria tabela Clientes
        // (Endereco_Logradouro, Endereco_Cidade, ...), conforme a migration NovaTabelaEnderecoCliente.
        // A PK da owned entity é a FK ClienteId, espelhando o snapshot das migrations.
        b.OwnsOne(x => x.Endereco, e =>
        {
            e.WithOwner().HasForeignKey("ClienteId");
            e.HasKey("ClienteId");

            e.Property(p => p.Logradouro).HasMaxLength(200).IsRequired();
            e.Property(p => p.Numero).HasMaxLength(20).IsRequired();
            e.Property(p => p.Complemento).HasMaxLength(100);
            e.Property(p => p.Bairro).HasMaxLength(100).IsRequired();
            e.Property(p => p.Cidade).HasMaxLength(100).IsRequired();
            e.Property(p => p.Estado).HasMaxLength(2).IsRequired();
            e.Property(p => p.Cep).HasMaxLength(8).IsRequired();
        });
        b.Navigation(x => x.Endereco).IsRequired();
    }
}
