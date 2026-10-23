using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.Categorias.ListCategorias;

public record ListCategoriasRequest(int Pagina = 1, int Tamanho = 10, bool? ApenasAtivas = null);

public interface IListCategoriasHandler
{
    Task<Result<PaginatedResponse<CategoriaResponse>>> Handle(ListCategoriasRequest request, CancellationToken ct = default);
}

public sealed class ListCategoriasHandler(ICategoriaRepository categorias) : IListCategoriasHandler
{
    public async Task<Result<PaginatedResponse<CategoriaResponse>>> Handle(ListCategoriasRequest request, CancellationToken ct = default)
    {
        var (items, total) = await categorias.ListAsync(request.Pagina, request.Tamanho, request.ApenasAtivas, ct);
        var dtos = items.Select(c => new CategoriaResponse(c.Id, c.Nome, c.Descricao, c.Ativo)).ToList();
        return Result<PaginatedResponse<CategoriaResponse>>.Ok(
            new PaginatedResponse<CategoriaResponse>(dtos, total, request.Pagina, request.Tamanho));
    }
}
