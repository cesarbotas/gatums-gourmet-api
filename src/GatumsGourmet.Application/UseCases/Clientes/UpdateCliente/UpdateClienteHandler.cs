using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Clientes.UpdateCliente;

public record UpdateClienteRequest(Guid Id, string Nome, string Email, string? Telefone, string? Cidade);

public interface IUpdateClienteHandler
{
    Task<Result<bool>> Handle(UpdateClienteRequest request, CancellationToken ct = default);
}

public sealed class UpdateClienteHandler(IClienteRepository clientes) : IUpdateClienteHandler
{
    public async Task<Result<bool>> Handle(UpdateClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await clientes.GetByIdAsync(request.Id, ct);
        if (cliente is null) return Result<bool>.Fail("Cliente não encontrado.");
        cliente.Nome = request.Nome;
        cliente.Email = request.Email;
        cliente.Telefone = request.Telefone;
        cliente.Cidade = request.Cidade;
        await clientes.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
