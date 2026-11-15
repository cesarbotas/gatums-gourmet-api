using System.Security.Claims;
using GatumsGourmet.Api.Authorization;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Estoque.AdjustEstoque;
using GatumsGourmet.Application.UseCases.V1.Estoque.ListEstoqueBaixo;
using GatumsGourmet.Application.UseCases.V1.Estoque.ListMovimentos;
using GatumsGourmet.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Estoque;

public static class EstoqueEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de estoque sob o grupo /produtos para manter o path
    /// semântico: /produtos/{id}/estoque, /produtos/{id}/estoque/movimentos.
    /// O alerta de estoque baixo fica em /estoque/baixo.
    /// </summary>
    public static void MapProdutoEstoqueEndpoints(RouteGroupBuilder group)
    {
        // GET /produtos/{id}/estoque — ver estoque atual
        group.MapGet("/{id:guid}/estoque", async (Guid id, IProdutoRepository produtos, CancellationToken ct) =>
        {
            var produto = await produtos.GetByIdAsync(id, ct);
            if (produto is null)
                return Results.Problem("Produto não encontrado.", statusCode: 400);
            return Results.Ok(new { id = produto.Id, estoque = produto.Estoque, estoqueMinimo = produto.EstoqueMinimo });
        }).RequirePermission("produtos:ler");

        // PUT /produtos/{id}/estoque — ajustar estoque (entrada/saída/ajuste)
        group.MapPut("/{id:guid}/estoque", async (
            Guid id,
            [FromBody] AdjustEstoqueRequest request,
            ClaimsPrincipal user,
            IAdjustEstoqueHandler handler,
            CancellationToken ct) =>
        {
            var usuarioId = user.GetUserId();
            var ajuste = request with { UsuarioId = usuarioId };
            return (await handler.Handle(id, ajuste, ct)).ToHttpResult();
        }).RequirePermission("estoque:gerenciar");

        // GET /produtos/{id}/estoque/movimentos — histórico de movimentos
        group.MapGet("/{id:guid}/estoque/movimentos", async (Guid id, IListMovimentosHandler handler, CancellationToken ct) =>
            (await handler.Handle(id, ct)).ToHttpResult())
            .RequirePermission("produtos:ler");
    }

    /// <summary>
    /// Mapeia endpoints de estoque globais (não atrelados a um produto específico).
    /// </summary>
    public static void Map(RouteGroupBuilder group)
    {
        // GET /estoque/baixo — produtos com estoque ≤ mínimo
        group.MapGet("/baixo", async (IListEstoqueBaixoHandler handler, CancellationToken ct) =>
            (await handler.Handle(ct)).ToHttpResult())
            .RequirePermission("estoque:gerenciar");
    }
}
