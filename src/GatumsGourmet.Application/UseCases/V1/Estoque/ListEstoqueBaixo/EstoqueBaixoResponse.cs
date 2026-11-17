namespace GatumsGourmet.Application.UseCases.V1.Estoque.ListEstoqueBaixo;

public record EstoqueBaixoResponse(
    Guid Id,
    string Nome,
    int Estoque,
    int EstoqueMinimo
);