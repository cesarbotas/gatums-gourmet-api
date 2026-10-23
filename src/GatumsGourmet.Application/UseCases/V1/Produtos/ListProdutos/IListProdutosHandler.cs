using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.ListProdutos;

public interface IListProdutosHandler
{
    Task<Result<PaginatedResponse<ProdutoSummaryResponse>>> Handle(ListProdutosRequest request, CancellationToken ct = default);
}
