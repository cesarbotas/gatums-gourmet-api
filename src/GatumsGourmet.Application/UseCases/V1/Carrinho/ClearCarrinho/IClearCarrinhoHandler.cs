using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.ClearCarrinho;

public interface IClearCarrinhoHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, CancellationToken ct = default);
}