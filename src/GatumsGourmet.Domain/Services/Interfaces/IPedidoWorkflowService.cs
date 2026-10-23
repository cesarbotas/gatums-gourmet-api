using GatumsGourmet.Domain.Enums.Pedidos;

namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IPedidoWorkflowService
{
    void ValidarTransicao(PedidoEtapa atual, PedidoEtapa proxima, PedidoActor actor);
}
