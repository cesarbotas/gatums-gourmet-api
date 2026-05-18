using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Clientes.ListClientes;

public record ListClientesRequest(int Page = 1, int PageSize = 10, string? Search = null);
public record ClienteResponse(Guid Id, string Nome, string Email, string? Telefone, string? Cidade, DateTime CriadoEm);

public interface IListClientesHandler
{
    Task<Result<PaginatedResponse<ClienteResponse>>> Handle(ListClientesRequest request, CancellationToken ct = default);
}

public sealed class ListClientesHandler(IClienteRepository clientes) : IListClientesHandler
{
    public async Task<Result<PaginatedResponse<ClienteResponse>>> Handle(ListClientesRequest request, CancellationToken ct = default)
    {
        var (items, total) = await clientes.ListAsync(request.Page, request.PageSize, request.Search, ct);
        var dtos = items.Select(c => new ClienteResponse(c.Id, c.Nome, c.Email, c.Telefone, c.Cidade, c.CriadoEm)).ToList();
        return Result<PaginatedResponse<ClienteResponse>>.Ok(new PaginatedResponse<ClienteResponse>(dtos, total, request.Page, request.PageSize));
    }
}
