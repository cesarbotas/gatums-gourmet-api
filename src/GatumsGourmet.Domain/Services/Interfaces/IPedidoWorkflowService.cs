using GatumsGourmet.Domain.Enums.Pedidos;

namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IPedidoWorkflowService
{
    void ValidarTransicao(PedidoEtapa atual, PedidoEtapa proxima, PedidoActor actor);

    /// <summary>
    /// Valida se um pedido na etapa <paramref name="atual"/> pode ser cancelado pelo
    /// <paramref name="actor"/>. Lança <see cref="Domain.Exceptions.BusinessRuleException"/>
    /// se a operação não for permitida.
    /// </summary>
    void ValidarCancelamento(PedidoEtapa atual, PedidoActor actor);
}
