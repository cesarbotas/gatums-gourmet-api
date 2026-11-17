using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.UpdateCarrinhoItemQuantidade;

public interface IUpdateCarrinhoItemQuantidadeHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, Guid produtoId, int quantidade, CancellationToken ct = default);
}