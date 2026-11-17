using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Carrinho.GetCarrinho;

public interface IGetCarrinhoHandler
{
    Task<Result<CarrinhoResponse>> Handle(Guid usuarioId, CancellationToken ct = default);
}