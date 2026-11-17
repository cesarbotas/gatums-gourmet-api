using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Clientes.DeleteCliente;

public interface IDeleteClienteHandler
{
    Task<Result<bool>> Handle(Guid id, CancellationToken ct = default);
}