namespace GatumsGourmet.Application.UseCases.Produtos.ListProdutos;

public record ProdutoSummaryResponse(Guid Id, string Nome, string Descricao, long CategoriaId, string CategoriaNome, decimal Preco, string FotoUrl, bool Disponivel, string Tipo);
