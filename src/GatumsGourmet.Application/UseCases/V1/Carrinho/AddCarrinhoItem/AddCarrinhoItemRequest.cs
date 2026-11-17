namespace GatumsGourmet.Application.UseCases.V1.Carrinho.AddCarrinhoItem;

public record AddCarrinhoItemRequest(Guid ProdutoId, int Quantidade);