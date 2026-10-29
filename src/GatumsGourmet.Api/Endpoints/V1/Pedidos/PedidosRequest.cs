using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Pedidos;

/// <summary>
/// Parâmetros de query da listagem de pedidos (<c>GET /pedidos</c>), agrupados
/// numa classe só e ligados via <c>[AsParameters]</c> — mesmo padrão de
/// <see cref="Produtos.ProdutosRequest"/>. O <c>ClaimsPrincipal</c> e a lógica
/// de autorização continuam no endpoint (não são parâmetros de query).
/// </summary>
public class PedidosRequest
{
    [FromQuery(Name = "pagina")]
    public int Pagina { get; set; } = 1;

    [FromQuery(Name = "tamanho")]
    public int Tamanho { get; set; } = 10;
}
