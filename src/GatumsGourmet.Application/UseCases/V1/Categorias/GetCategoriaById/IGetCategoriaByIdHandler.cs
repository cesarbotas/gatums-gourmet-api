using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.GetCategoriaById;

public interface IGetCategoriaByIdHandler
{
    Task<Result<GetCategoriaByIdResponse>> Handle(long id, CancellationToken ct = default);
}