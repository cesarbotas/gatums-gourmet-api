using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Clientes.DeleteCliente;

public interface IDeleteClienteHandler
{
    Task<Result<bool>> Handle(Guid id, CancellationToken ct = default);
}

public sealed class DeleteClienteHandler(IClienteRepository clientes) : IDeleteClienteHandler
{
    public async Task<Result<bool>> Handle(Guid id, CancellationToken ct = default)
    {
        var cliente = await clientes.GetByIdAsync(id, ct);
        if (cliente is null) return Result<bool>.Fail("Cliente não encontrado.");
        await clientes.DeleteAsync(cliente, ct);
        await clientes.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

