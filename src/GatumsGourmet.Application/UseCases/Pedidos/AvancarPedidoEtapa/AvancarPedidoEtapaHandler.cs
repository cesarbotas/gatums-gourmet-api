using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Exceptions;

namespace GatumsGourmet.Application.UseCases.Pedidos.AvancarPedidoEtapa;

public record AvancarPedidoEtapaRequest(PedidoEtapa ProximaEtapa, string? Observacao, EntregaTipo? EntregaTipo);

public interface IAvancarPedidoEtapaHandler
{
    Task<Result<bool>> Handle(Guid pedidoId, AvancarPedidoEtapaRequest request, PedidoActor actor, CancellationToken ct = default);
}

public sealed class AvancarPedidoEtapaHandler(IPedidoRepository pedidos, IPedidoWorkflowService workflow, IClock clock) : IAvancarPedidoEtapaHandler
{
    public async Task<Result<bool>> Handle(Guid pedidoId, AvancarPedidoEtapaRequest request, PedidoActor actor, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(pedidoId, ct);
        if (pedido is null) return Result<bool>.Fail("Pedido não encontrado.");

        if (request.ProximaEtapa == PedidoEtapa.Entregue && request.EntregaTipo is null)
            throw new BusinessRuleException("EntregaTipo é obrigatório para a etapa 'entregue'.");

        workflow.ValidarTransicao(pedido.StatusAtual, request.ProximaEtapa, actor);

        pedido.StatusAtual = request.ProximaEtapa;
        pedido.Timeline.Add(new PedidoTimelineEvento { Id = Guid.NewGuid(), PedidoId = pedido.Id, Etapa = request.ProximaEtapa, Em = clock.UtcNow, Por = actor, Observacao = request.Observacao, EntregaTipo = request.EntregaTipo });

        await pedidos.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

