namespace GatumsGourmet.Application.UseCases.V1.Estoque.AdjustEstoque;

public record AdjustEstoqueResponse(
    int EstoqueAnterior,
    int EstoqueNovo
);