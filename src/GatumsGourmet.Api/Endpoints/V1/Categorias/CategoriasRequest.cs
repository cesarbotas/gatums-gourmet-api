using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Categorias;

/// <summary>
/// Parâmetros de query da listagem de categorias (<c>GET /categorias</c>),
/// agrupados numa classe só e ligados via <c>[AsParameters]</c> — mesmo padrão
/// de <see cref="Produtos.ProdutosRequest"/>.
/// </summary>
public class CategoriasRequest
{
    [FromQuery(Name = "pagina")]
    public int Pagina { get; set; } = 1;

    [FromQuery(Name = "tamanho")]
    public int Tamanho { get; set; } = 10;

    /// <summary><c>true</c> traz só as categorias ativas (catálogo).</summary>
    [FromQuery(Name = "apenasAtivas")]
    public bool? ApenasAtivas { get; set; }
}
