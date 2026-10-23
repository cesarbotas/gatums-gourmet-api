using GatumsGourmet.Domain.Enums.Produtos;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.ListProdutos;

public record ListProdutosRequest(int Page = 1, int PageSize = 10, string? Search = null, long? CategoriaId = null, TipoProduto? Tipo = null, bool? Disponivel = null);
