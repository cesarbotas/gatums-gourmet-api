using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.DeleteCategoria;

public interface IDeleteCategoriaHandler
{
    Task<Result<bool>> Handle(long id, CancellationToken ct = default);
}