using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Clientes;

/// <summary>
/// Parâmetros de query da listagem de clientes (<c>GET /clientes</c>),
/// agrupados numa classe só e ligados via <c>[AsParameters]</c> — mesmo padrão
/// de <see cref="Produtos.ProdutosRequest"/>.
/// </summary>
public class ClientesRequest
{
    [FromQuery(Name = "pagina")]
    public int Pagina { get; set; } = 1;

    [FromQuery(Name = "tamanho")]
    public int Tamanho { get; set; } = 10;

    /// <summary>Busca textual por nome/email.</summary>
    [FromQuery(Name = "search")]
    public string? Search { get; set; }
}
