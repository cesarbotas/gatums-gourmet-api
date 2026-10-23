using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.UpdateCarrinhoItemQuantidade;

public record UpdateCarrinhoItemQuantidadeRequest(int Quantidade);

public interface IUpdateCarrinhoItemQuantidadeHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, Guid produtoId, int quantidade, CancellationToken ct = default);
}

public sealed class UpdateCarrinhoItemQuantidadeHandler(ICarrinhoRepository carrinho) : IUpdateCarrinhoItemQuantidadeHandler
{
    public async Task<Result<bool>> Handle(Guid usuarioId, Guid produtoId, int quantidade, CancellationToken ct = default)
    {
        var item = await carrinho.GetItemAsync(usuarioId, produtoId, ct);
        if (item is null) return Result<bool>.Fail("Item não encontrado no carrinho.");
        if (quantidade <= 0)
            await carrinho.RemoveAsync(item, ct);
        else
            item.Quantidade = quantidade;
        await carrinho.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

