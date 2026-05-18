using GatumsGourmet.Domain.Enums;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Interfaces;

namespace GatumsGourmet.Domain.Services;

public class PedidoWorkflowService : IPedidoWorkflowService
{
    private static readonly Dictionary<PedidoEtapa, (PedidoEtapa Proxima, PedidoActor Actor)[]> _transicoes = new()
    {
        [PedidoEtapa.Criado]            = [(PedidoEtapa.Aprovado, PedidoActor.Admin)],
        [PedidoEtapa.Aprovado]          = [(PedidoEtapa.Pagamento, PedidoActor.Cliente)],
        [PedidoEtapa.Pagamento]         = [(PedidoEtapa.PagamentoAprovado, PedidoActor.Admin)],
        [PedidoEtapa.PagamentoAprovado] = [(PedidoEtapa.EnvioPendente, PedidoActor.Admin)],
        [PedidoEtapa.EnvioPendente]     = [(PedidoEtapa.Entregue, PedidoActor.Admin)],
        [PedidoEtapa.Entregue]          = [(PedidoEtapa.Finalizado, PedidoActor.Admin)],
        [PedidoEtapa.Finalizado]        = [],
    };

    public void ValidarTransicao(PedidoEtapa atual, PedidoEtapa proxima, PedidoActor actor)
    {
        if (!_transicoes.TryGetValue(atual, out var permitidas))
            throw new BusinessRuleException($"Etapa '{atual}' não possui transições definidas.");

        var match = permitidas.FirstOrDefault(t => t.Proxima == proxima);
        if (match == default)
            throw new BusinessRuleException($"Transição de '{atual}' para '{proxima}' não é permitida.");

        if (match.Actor != actor)
            throw new BusinessRuleException($"Apenas '{match.Actor}' pode avançar para '{proxima}'.");
    }
}
