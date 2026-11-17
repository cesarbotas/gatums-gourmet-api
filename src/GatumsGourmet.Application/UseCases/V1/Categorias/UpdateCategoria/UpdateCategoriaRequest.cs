namespace GatumsGourmet.Application.UseCases.V1.Categorias.UpdateCategoria;

public record UpdateCategoriaRequest(
    long Id,
    string Nome,
    string Descricao,
    bool Ativo
);
