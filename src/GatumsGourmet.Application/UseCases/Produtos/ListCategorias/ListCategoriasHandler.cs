using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Produtos.ListCategorias;

public interface IListCategoriasHandler
{
    Task<Result<IReadOnlyList<string>>> Handle(CancellationToken ct = default);
}

public sealed class ListCategoriasHandler(IProdutoRepository produtos) : IListCategoriasHandler
{
    public async Task<Result<IReadOnlyList<string>>> Handle(CancellationToken ct = default)
    {
        var categorias = await produtos.ListCategoriasAsync(ct);
        return Result<IReadOnlyList<string>>.Ok(categorias);
    }
}
