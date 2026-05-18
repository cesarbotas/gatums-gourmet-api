namespace GatumsGourmet.Application.UseCases.Produtos.ListProdutos;

public record ProdutoSummaryResponse(Guid Id, string Nome, string Descricao, string Categoria, decimal Preco, string FotoUrl, bool Disponivel, string Tipo);
