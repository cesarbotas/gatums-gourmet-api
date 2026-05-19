using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Produtos.ListProdutos;

public interface IListProdutosHandler
{
    Task<Result<PaginatedResponse<ProdutoSummaryResponse>>> Handle(ListProdutosRequest request, CancellationToken ct = default);
}
