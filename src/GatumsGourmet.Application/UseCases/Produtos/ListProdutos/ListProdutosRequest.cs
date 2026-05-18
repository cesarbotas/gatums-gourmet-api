using GatumsGourmet.Domain.Enums.Produtos;

namespace GatumsGourmet.Application.UseCases.Produtos.ListProdutos;

public record ListProdutosRequest(int Page = 1, int PageSize = 10, string? Search = null, string? Categoria = null, TipoProduto? Tipo = null, bool? Disponivel = null);
