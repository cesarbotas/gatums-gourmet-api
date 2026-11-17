using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.UpdateCategoria;

public interface IUpdateCategoriaHandler
{
    Task<Result<bool>> Handle(UpdateCategoriaRequest request, CancellationToken ct = default);
}
