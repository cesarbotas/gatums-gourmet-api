using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Interfaces;

namespace GatumsGourmet.Application.UseCases.Clientes.CreateCliente;

public record CreateClienteRequest(string Nome, string Email, string? Telefone, string? Cidade);
public record CreateClienteResponse(Guid Id);

public class CreateClienteValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
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
        var cliente = new Cliente { Id = Guid.NewGuid(), Nome = request.Nome, Email = request.Email, Telefone = request.Telefone, Cidade = request.Cidade, CriadoEm = clock.UtcNow };
        await clientes.AddAsync(cliente, ct);
        await clientes.SaveChangesAsync(ct);
        return Result<CreateClienteResponse>.Ok(new CreateClienteResponse(cliente.Id));
    }
}
