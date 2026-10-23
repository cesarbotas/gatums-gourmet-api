namespace GatumsGourmet.Application.UseCases.V1.Categorias;

public record CategoriaResponse(long Id, string Nome, string? Descricao, bool Ativo);
