namespace GatumsGourmet.Application.UseCases.V1.Categorias.CreateCategoria;

public record CreateCategoriaRequest(string Nome, string? Descricao, bool Ativo = true);