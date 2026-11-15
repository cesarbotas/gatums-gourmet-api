using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Estoque;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Estoque;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos.CancelarPedido;

/// <summary>
/// Cancela um pedido:
/// 1. Valida permissão de cancelamento (actor + etapa atual).
/// 2. Cliente só pode cancelar o próprio pedido.
/// 3. Devolve todos os itens ao estoque com movimento do tipo Ajuste (cancelamento).
/// 4. Marca a transação de pagamento como Recusada (se ainda pendente).
/// 5. Avança o pedido para <see cref="PedidoEtapa.Cancelado"/> e registra na timeline.
/// </summary>
public sealed class CancelarPedidoHandler(
    IPedidoRepository pedidos,
    IProdutoRepository produtos,
    IMovimentoEstoqueRepository movimentos,
    ITransacaoRepository transacoes,
    IPedidoWorkflowService workflow,
    IClock clock,
    ILogger<CancelarPedidoHandler> logger) : ICancelarPedidoHandler
{
    public async Task<Result<CancelarPedidoResponse>> Handle(
        Guid pedidoId,
        CancelarPedidoRequest request,
        Guid requestingUserId,
        PedidoActor actor,
        CancellationToken ct = default)
    {
        // ── 1. Carregar pedido ─────────────────────────────────
        var pedido = await pedidos.GetByIdAsync(pedidoId, ct);
        if (pedido is null)
            return Result<CancelarPedidoResponse>.Fail("Pedido não encontrado.");

        // ── 2. Verificar propriedade (cliente só cancela o próprio) ──
        if (actor == PedidoActor.Cliente && pedido.UsuarioId != requestingUserId)
            return Result<CancelarPedidoResponse>.Fail("Acesso negado: pedido não pertence ao usuário.");

        // ── 3. Validar se a etapa permite cancelamento ─────────
        var etapaAnterior = pedido.StatusAtual;
        workflow.ValidarCancelamento(etapaAnterior, actor); // lança BusinessRuleException se não puder

        // ── 4. Devolver itens ao estoque ───────────────────────
        var produtoIds = pedido.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtosDict = (await produtos.GetByIdsAsync(produtoIds, ct)).ToDictionary(p => p.Id);
        var agora = clock.UtcNow;
        var movimentosEstoque = new List<MovimentoEstoque>();

        foreach (var item in pedido.Itens)
        {
            if (!produtosDict.TryGetValue(item.ProdutoId, out var produto))
            {
                // Produto pode ter sido excluído após a venda — log e segue
                logger.LogWarning(
                    "Produto {ProdutoId} do pedido {PedidoId} não encontrado para estorno de estoque.",
                    item.ProdutoId, pedidoId);
                continue;
            }

            produto.Estoque += item.Quantidade;

            movimentosEstoque.Add(new MovimentoEstoque
            {
                Id               = Guid.NewGuid(),
                ProdutoId        = produto.Id,
                Tipo             = TipoMovimentoEstoque.Ajuste,
                Quantidade       = item.Quantidade,          // positivo = entrada
                SaldoResultante  = produto.Estoque,
                PedidoId         = pedidoId,
                UsuarioId        = requestingUserId,
                Observacao       = $"Estorno por cancelamento do pedido {pedidoId.ToString()[..8]} — {request.Motivo ?? "sem motivo"}",
                Data             = agora
            });
        }

        // ── 5. Cancelar transação de pagamento pendente ────────
        var transacao = await transacoes.GetByPedidoIdAsync(pedidoId, ct);
        if (transacao is not null && transacao.Status == StatusTransacao.Pendente)
        {
            transacao.Status       = StatusTransacao.Recusado;
            transacao.MensagemErro = $"Pedido cancelado: {request.Motivo ?? "sem motivo"}";
            transacao.AtualizadoEm = agora;
        }

        // ── 6. Cancelar pedido e registrar na timeline ─────────
        pedido.StatusAtual = PedidoEtapa.Cancelado;
        pedido.Timeline.Add(new PedidoTimelineEvento
        {
            Id          = Guid.NewGuid(),
            PedidoId    = pedidoId,
            Etapa       = PedidoEtapa.Cancelado,
            Em          = agora,
            Por         = actor,
            Observacao  = request.Motivo
        });

        // ── 7. Persistir tudo ──────────────────────────────────
        await movimentos.AddRangeAsync(movimentosEstoque, ct);
        await pedidos.SaveChangesAsync(ct);
        if (transacao is not null)
            await transacoes.SaveChangesAsync(ct);

        logger.LogInformation(
            "Pedido cancelado. PedidoId={PedidoId} EtapaAnterior={Etapa} Actor={Actor} " +
            "ItensEstornados={Itens} Motivo={Motivo}",
            pedidoId, etapaAnterior, actor, movimentosEstoque.Count, request.Motivo);

        return Result<CancelarPedidoResponse>.Ok(new CancelarPedidoResponse(
            pedidoId,
            etapaAnterior,
            movimentosEstoque.Count));
    }
}
