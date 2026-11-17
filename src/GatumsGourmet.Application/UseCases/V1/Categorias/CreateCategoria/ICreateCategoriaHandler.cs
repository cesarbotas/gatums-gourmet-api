using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.CreateCategoria;

public interface ICreateCategoriaHandler
{
    Task<Result<CreateCategoriaResponse>> Handle(CreateCategoriaRequest request, CancellationToken ct = default);
}
