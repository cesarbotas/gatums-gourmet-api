namespace GatumsGourmet.Application.UseCases.V1.Produtos.UpdateProduto;

public record UpdateProdutoRequest(Guid Id, string Nome, string Descricao, long CategoriaId, decimal Preco, string FotoUrl, bool Disponivel);
