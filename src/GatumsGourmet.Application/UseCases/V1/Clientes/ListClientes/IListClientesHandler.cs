using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Clientes.ListClientes;

public interface IListClientesHandler
{
    Task<Result<PaginatedResponse<ClienteResponse>>> Handle(ListClientesRequest request, CancellationToken ct = default);
}
