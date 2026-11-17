using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.GetCategoriaById;

public sealed class GetCategoriaByIdHandler(ICategoriaRepository categorias) : IGetCategoriaByIdHandler
{
    public async Task<Result<GetCategoriaByIdResponse>> Handle(long id, CancellationToken ct = default)
    {
        var c = await categorias.GetByIdAsync(id, ct);
        if (c is null) return Result<GetCategoriaByIdResponse>.Fail("Categoria não encontrada.");
        return Result<GetCategoriaByIdResponse>.Ok(new GetCategoriaByIdResponse(c.Id, c.Nome, c.Descricao));
    }
}
