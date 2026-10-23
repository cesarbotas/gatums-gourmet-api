using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Carrinho.ClearCarrinho;

public interface IClearCarrinhoHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, CancellationToken ct = default);
}

public sealed class ClearCarrinhoHandler(ICarrinhoRepository carrinho) : IClearCarrinhoHandler
{
    public async Task<Result<bool>> Handle(Guid usuarioId, CancellationToken ct = default)
    {
        await carrinho.ClearAsync(usuarioId, ct);
        await carrinho.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

