namespace GatumsGourmet.Application.UseCases.V1.Categorias.GetCategoriaById;

public record GetCategoriaByIdResponse(
    long Id
    , string Nome
    , string? Descricao
);