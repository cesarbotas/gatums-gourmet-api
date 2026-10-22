using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Categorias.GetCategoriaById;

public interface IGetCategoriaByIdHandler
{
    Task<Result<CategoriaResponse>> Handle(long id, CancellationToken ct = default);
}

public sealed class GetCategoriaByIdHandler(ICategoriaRepository categorias) : IGetCategoriaByIdHandler
{
    public async Task<Result<CategoriaResponse>> Handle(long id, CancellationToken ct = default)
    {
        var c = await categorias.GetByIdAsync(id, ct);
        if (c is null) return Result<CategoriaResponse>.Fail("Categoria não encontrada.");
        return Result<CategoriaResponse>.Ok(new CategoriaResponse(c.Id, c.Nome, c.Descricao, c.Ativo));
    }
}

