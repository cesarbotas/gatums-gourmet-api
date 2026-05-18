using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Enums;

namespace GatumsGourmet.Application.UseCases.Produtos.ListProdutos;

public record ListProdutosRequest(int Page = 1, int PageSize = 10, string? Search = null, string? Categoria = null, TipoProduto? Tipo = null, bool? Disponivel = null);
public record ProdutoSummaryResponse(Guid Id, string Nome, string Descricao, string Categoria, decimal Preco, string FotoUrl, bool Disponivel, string Tipo);

public interface IListProdutosHandler
{
    Task<Result<PaginatedResponse<ProdutoSummaryResponse>>> Handle(ListProdutosRequest request, CancellationToken ct = default);
}

public sealed class ListProdutosHandler(IProdutoRepository produtos) : IListProdutosHandler
{
    public async Task<Result<PaginatedResponse<ProdutoSummaryResponse>>> Handle(ListProdutosRequest request, CancellationToken ct = default)
    {
        var (items, total) = await produtos.ListAsync(request.Page, request.PageSize, request.Search, request.Categoria, request.Tipo, request.Disponivel, ct);
        var dtos = items.Select(p => new ProdutoSummaryResponse(p.Id, p.Nome, p.Descricao, p.Categoria, p.Preco, p.FotoUrl, p.Disponivel, p.Tipo.ToString().ToLower())).ToList();
        return Result<PaginatedResponse<ProdutoSummaryResponse>>.Ok(new PaginatedResponse<ProdutoSummaryResponse>(dtos, total, request.Page, request.PageSize));
    }
}
