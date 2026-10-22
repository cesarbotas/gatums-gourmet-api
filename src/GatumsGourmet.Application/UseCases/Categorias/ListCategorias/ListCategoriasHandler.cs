using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Categorias.ListCategorias;

public interface IListCategoriasHandler
{
    Task<Result<IReadOnlyList<CategoriaResponse>>> Handle(bool? apenasAtivas, CancellationToken ct = default);
}

public sealed class ListCategoriasHandler(ICategoriaRepository categorias) : IListCategoriasHandler
{
    public async Task<Result<IReadOnlyList<CategoriaResponse>>> Handle(bool? apenasAtivas, CancellationToken ct = default)
    {
        var items = await categorias.ListAsync(apenasAtivas, ct);
        var dtos = (IReadOnlyList<CategoriaResponse>)items
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Descricao, c.Ativo))
            .ToList();
        return Result<IReadOnlyList<CategoriaResponse>>.Ok(dtos);
    }
}

