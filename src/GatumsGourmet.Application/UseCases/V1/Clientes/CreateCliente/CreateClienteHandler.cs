using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Clientes;

namespace GatumsGourmet.Application.UseCases.V1.Clientes.CreateCliente;

public sealed class CreateClienteHandler(IClienteRepository clientes, IClock clock) : ICreateClienteHandler
{
    public async Task<Result<CreateClienteResponse>> Handle(CreateClienteRequest request, CancellationToken ct = default)
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Email = request.Email,
            Telefone = request.Telefone,
            CriadoEm = clock.UtcNow,
            Endereco = new Endereco
            {
                Id = Guid.NewGuid(),
                Logradouro = request.Endereco.Logradouro,
                Numero = request.Endereco.Numero,
                Complemento = request.Endereco.Complemento,
                Bairro = request.Endereco.Bairro,
                Cidade = request.Endereco.Cidade,
                Estado = request.Endereco.Estado,
                Cep = request.Endereco.Cep
            }
        };

        await clientes.AddAsync(cliente, ct);

        await clientes.SaveChangesAsync(ct);
        
        return Result<CreateClienteResponse>.Ok(new CreateClienteResponse(cliente.Id));
    }
}
