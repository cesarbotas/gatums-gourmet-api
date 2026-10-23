using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Carrinho.RemoveCarrinhoItem;

public interface IRemoveCarrinhoItemHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, Guid produtoId, CancellationToken ct = default);
}

public sealed class RemoveCarrinhoItemHandler(ICarrinhoRepository carrinho) : IRemoveCarrinhoItemHandler
{
    public async Task<Result<bool>> Handle(Guid usuarioId, Guid produtoId, CancellationToken ct = default)
    {
        var item = await carrinho.GetItemAsync(usuarioId, produtoId, ct);
        if (item is null) return Result<bool>.Fail("Item não encontrado no carrinho.");
        await carrinho.RemoveAsync(item, ct);
        await carrinho.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

