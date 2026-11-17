using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.RemoveCarrinhoItem;

public interface IRemoveCarrinhoItemHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, Guid produtoId, CancellationToken ct = default);
}
