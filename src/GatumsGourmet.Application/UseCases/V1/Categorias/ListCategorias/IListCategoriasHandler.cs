using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.ListCategorias;

public interface IListCategoriasHandler
{
    Task<Result<PaginatedResponse<ListCategoriasResponse>>> Handle(ListCategoriasRequest request, CancellationToken ct = default);
}
