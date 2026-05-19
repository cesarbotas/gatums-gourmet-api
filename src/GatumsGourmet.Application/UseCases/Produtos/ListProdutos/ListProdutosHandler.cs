using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;

namespace GatumsGourmet.Application.UseCases.Produtos.ListProdutos;

public sealed class ListProdutosHandler(IProdutoRepository produtos) : IListProdutosHandler
{
    public async Task<Result<PaginatedResponse<ProdutoSummaryResponse>>> Handle(ListProdutosRequest request, CancellationToken ct = default)
    {
        var (items, total) = await produtos.ListAsync(request.Page, request.PageSize, request.Search, request.Categoria, request.Tipo, request.Disponivel, ct);
        var dtos = items.Select(p => new ProdutoSummaryResponse(p.Id, p.Nome, p.Descricao, p.Categoria, p.Preco, p.FotoUrl, p.Disponivel, p.Tipo.ToString().ToLower())).ToList();
        return Result<PaginatedResponse<ProdutoSummaryResponse>>.Ok(new PaginatedResponse<ProdutoSummaryResponse>(dtos, total, request.Page, request.PageSize));
    }
}
