namespace GatumsGourmet.Application.UseCases.Produtos.UpdateProduto;

public record UpdateProdutoRequest(Guid Id, string Nome, string Descricao, string Categoria, decimal Preco, string FotoUrl, bool Disponivel);
