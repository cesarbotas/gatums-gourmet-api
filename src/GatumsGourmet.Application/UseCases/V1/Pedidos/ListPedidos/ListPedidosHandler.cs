using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Pedidos;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos.ListPedidos;

public record ListPedidosRequest(int Page = 1, int PageSize = 10);

public interface IListPedidosHandler
{
    Task<Result<PaginatedResponse<PedidoResponse>>> Handle(Guid? filtroUsuarioId, ListPedidosRequest request, CancellationToken ct = default);
}

public sealed class ListPedidosHandler(IPedidoRepository pedidos) : IListPedidosHandler
{
    public async Task<Result<PaginatedResponse<PedidoResponse>>> Handle(Guid? filtroUsuarioId, ListPedidosRequest request, CancellationToken ct = default)
    {
        var (items, total) = await pedidos.ListAsync(filtroUsuarioId, request.Page, request.PageSize, ct);
        var dtos = items.Select(MapPedido).ToList();
        return Result<PaginatedResponse<PedidoResponse>>.Ok(new PaginatedResponse<PedidoResponse>(dtos, total, request.Page, request.PageSize));
    }

    internal static PedidoResponse MapPedido(Pedido p) => new(
        p.Id, p.UsuarioId, p.ClienteNome,
        p.Itens.Select(i => new PedidoItemResponse(i.ProdutoId, i.Nome, i.Preco, i.FotoUrl, i.Quantidade)).ToList(),
        p.Total, p.Data,
        p.StatusAtual.ToString().ToLower(),
        p.Timeline.OrderBy(t => t.Em).Select(t => new TimelineEventoResponse(t.Etapa.ToString().ToLower(), t.Em, t.Por.ToString().ToLower(), t.Observacao, t.EntregaTipo?.ToString().ToLower())).ToList(),
        p.Pagamento is null ? null : new PedidoPagamentoResponse(p.Pagamento.TransacaoId, p.Pagamento.Resumo, p.Pagamento.AprovadoEm));
}

