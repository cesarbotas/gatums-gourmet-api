using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.AddCarrinhoItem;

public interface IAddCarrinhoItemHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, AddCarrinhoItemRequest request, CancellationToken ct = default);
}