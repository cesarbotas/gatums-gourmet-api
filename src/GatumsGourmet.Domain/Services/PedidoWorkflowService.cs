using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Services.Interfaces;

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
        [PedidoEtapa.Cancelado]         = [],
    };

    /// <summary>
    /// Etapas em que um cancelamento é permitido por actor.
    /// Cliente cancela apenas enquanto o pedido não foi aprovado pelo admin (pré-pagamento).
    /// Admin pode cancelar até o momento em que o envio inicia.
    /// </summary>
    private static readonly HashSet<PedidoEtapa> _cancelaveisCliente = new()
    {
        PedidoEtapa.Criado,
        PedidoEtapa.Aprovado,
        PedidoEtapa.Pagamento
    };

    private static readonly HashSet<PedidoEtapa> _cancelaveisAdmin = new()
    {
        PedidoEtapa.Criado,
        PedidoEtapa.Aprovado,
        PedidoEtapa.Pagamento,
        PedidoEtapa.PagamentoAprovado,
        PedidoEtapa.EnvioPendente
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

    public void ValidarCancelamento(PedidoEtapa atual, PedidoActor actor)
    {
        if (atual == PedidoEtapa.Cancelado)
            throw new BusinessRuleException("Pedido já está cancelado.");

        if (atual == PedidoEtapa.Finalizado)
            throw new BusinessRuleException("Pedido finalizado não pode ser cancelado.");

        if (atual == PedidoEtapa.Entregue)
            throw new BusinessRuleException("Pedido entregue não pode ser cancelado. Solicite um estorno pelo suporte.");

        var cancelaveis = actor == PedidoActor.Admin ? _cancelaveisAdmin : _cancelaveisCliente;

        if (!cancelaveis.Contains(atual))
            throw new BusinessRuleException(
                actor == PedidoActor.Cliente
                    ? $"Cliente não pode cancelar um pedido na etapa '{atual}'. Contate o suporte."
                    : $"Pedido na etapa '{atual}' não pode ser cancelado.");
    }
}
