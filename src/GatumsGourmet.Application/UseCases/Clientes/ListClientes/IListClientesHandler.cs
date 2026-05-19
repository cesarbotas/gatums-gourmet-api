using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Clientes.ListClientes;

public interface IListClientesHandler
{
    Task<Result<PaginatedResponse<ClienteResponse>>> Handle(ListClientesRequest request, CancellationToken ct = default);
}
