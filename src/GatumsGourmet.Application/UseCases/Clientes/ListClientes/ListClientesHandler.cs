using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Clientes.ListClientes;

public sealed class ListClientesHandler(IClienteRepository clientes) : IListClientesHandler
{
    public async Task<Result<PaginatedResponse<ClienteResponse>>> Handle(ListClientesRequest request, CancellationToken ct = default)
    {
        var (items, total) = await clientes.ListAsync(request.Page, request.PageSize, request.Search, ct);

        var dtos = items.Select(c =>
            new ClienteResponse(
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Telefone,
                    new EnderecoResponse(
                        c.Endereco.Id,
                        c.Id,
                        c.Endereco.Logradouro,
                        c.Endereco.Numero,
                        c.Endereco.Complemento,
                        c.Endereco.Bairro,
                        c.Endereco.Cidade,
                        c.Endereco.Estado,
                        c.Endereco.Cep),
                    c.CriadoEm)).ToList();
        
        return Result<PaginatedResponse<ClienteResponse>>.Ok(new PaginatedResponse<ClienteResponse>(dtos, total, request.Page, request.PageSize));
    }
}

