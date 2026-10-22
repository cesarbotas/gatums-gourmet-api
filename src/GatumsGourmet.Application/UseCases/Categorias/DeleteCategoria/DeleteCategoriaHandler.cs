using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Categorias.DeleteCategoria;

public interface IDeleteCategoriaHandler
{
    Task<Result<bool>> Handle(long id, CancellationToken ct = default);
}

public sealed class DeleteCategoriaHandler(ICategoriaRepository categorias) : IDeleteCategoriaHandler
{
    public async Task<Result<bool>> Handle(long id, CancellationToken ct = default)
    {
        var categoria = await categorias.GetByIdAsync(id, ct);
        if (categoria is null) return Result<bool>.Fail("Categoria não encontrada.");

        // Regra: não permite excluir categoria com produtos vinculados.
        if (await categorias.TemProdutosVinculadosAsync(id, ct))
            return Result<bool>.Fail("Não é possível excluir: categoria possui produtos vinculados.");

        await categorias.DeleteAsync(categoria, ct);
        await categorias.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

