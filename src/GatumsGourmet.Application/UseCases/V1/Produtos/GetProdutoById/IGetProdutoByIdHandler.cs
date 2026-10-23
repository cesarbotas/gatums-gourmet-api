using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.GetProdutoById;

public interface IGetProdutoByIdHandler
{
    Task<Result<ProdutoDetailResponse>> Handle(Guid id, CancellationToken ct = default);
}
