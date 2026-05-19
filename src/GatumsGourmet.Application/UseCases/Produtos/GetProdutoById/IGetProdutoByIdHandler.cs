using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Produtos.GetProdutoById;

public interface IGetProdutoByIdHandler
{
    Task<Result<ProdutoDetailResponse>> Handle(Guid id, CancellationToken ct = default);
}
