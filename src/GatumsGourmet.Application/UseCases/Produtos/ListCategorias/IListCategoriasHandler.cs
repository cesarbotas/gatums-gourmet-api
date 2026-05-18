using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Produtos.ListCategorias;

public interface IListCategoriasHandler
{
    Task<Result<IReadOnlyList<string>>> Handle(CancellationToken ct = default);
}
