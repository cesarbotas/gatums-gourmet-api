using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Clientes.ListClientes;

public sealed class ListClientesHandler(IClienteRepository clientes) : IListClientesHandler
{
    public async Task<Result<PaginatedResponse<ClienteResponse>>> Handle(ListClientesRequest request, CancellationToken ct = default)
    {
        var (items, total) = await clientes.ListAsync(request.Page, request.PageSize, request.Search, ct);
        var dtos = items.Select(c => new ClienteResponse(c.Id, c.Nome, c.Email, c.Telefone, c.Cidade, c.CriadoEm)).ToList();
        return Result<PaginatedResponse<ClienteResponse>>.Ok(new PaginatedResponse<ClienteResponse>(dtos, total, request.Page, request.PageSize));
    }
}
