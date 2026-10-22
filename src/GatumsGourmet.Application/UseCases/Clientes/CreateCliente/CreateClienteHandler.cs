using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Clientes;

namespace GatumsGourmet.Application.UseCases.Clientes.CreateCliente;

public record EnderecoRequest(
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep
);

public record CreateClienteRequest(
    string Nome,
    string Email,
    string? Telefone,
    EnderecoRequest Endereco
);

public record CreateClienteResponse(Guid Id);

public class CreateClienteValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Endereco).NotNull();

        RuleFor(x => x.Endereco.Logradouro)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Endereco.Numero)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Endereco.Bairro)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Endereco.Cidade)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Endereco.Estado)
            .NotEmpty()
            .Length(2);

        RuleFor(x => x.Endereco.Cep)
            .NotEmpty()
            .MaximumLength(8);
    }
}

public interface ICreateClienteHandler
{
    Task<Result<CreateClienteResponse>> Handle(CreateClienteRequest request, CancellationToken ct = default);
}

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

