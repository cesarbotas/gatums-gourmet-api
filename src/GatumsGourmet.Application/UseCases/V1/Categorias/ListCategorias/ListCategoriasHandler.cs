using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;
public sealed class ListCategoriasHandler(ICategoriaRepository categorias) : IListCategoriasHandler
{
    public async Task<Result<PaginatedResponse<ListCategoriasResponse>>> Handle(ListCategoriasRequest request, CancellationToken ct = default)
    {
        var (items, total) = await categorias.ListAsync(request.Pagina, request.Tamanho, request.ApenasAtivas, ct);
        var dtos = items.Select(c => new ListCategoriasResponse(c.Id, c.Nome, c.Descricao, c.Ativo)).ToList();
        return Result<PaginatedResponse<ListCategoriasResponse>>.Ok(
            new PaginatedResponse<ListCategoriasResponse>(dtos, total, request.Pagina, request.Tamanho));
    }
}
