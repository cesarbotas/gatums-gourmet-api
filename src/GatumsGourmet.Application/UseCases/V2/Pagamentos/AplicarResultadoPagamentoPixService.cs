using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V2.Pagamentos;

public interface IAplicarResultadoPagamentoPixService
{
    /// <returns><c>true</c> se algo mudou e foi salvo.</returns>
    Task<bool> AplicarAsync(TransacaoPagamento transacao, PixOrderStatus status, string origem, CancellationToken ct = default);
}

public sealed class AplicarResultadoPagamentoPixService(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IClock clock,
    ILogger<AplicarResultadoPagamentoPixService> logger) : IAplicarResultadoPagamentoPixService
{
    public async Task<bool> AplicarAsync(TransacaoPagamento transacao, PixOrderStatus status, string origem, CancellationToken ct = default)
    {
        // Idempotência: só transações pendentes são alteradas.
        if (transacao.Status != StatusTransacao.Pendente)
            return false;

        var novoStatus = PixOrderStatusMapper.ToStatusTransacao(status);
        if (novoStatus is null)
            return false;

        var agora = clock.UtcNow;
        transacao.Status = novoStatus.Value;
        transacao.AtualizadoEm = agora;

        if (novoStatus == StatusTransacao.Aprovado)
        {
            var pedido = await pedidos.GetByIdAsync(transacao.PedidoId, ct);
            if (pedido is not null && pedido.StatusAtual is PedidoEtapa.Criado or PedidoEtapa.Aprovado)
            {
                pedido.Pagamento = new PedidoPagamento
                {
                    PedidoId = pedido.Id,
                    TransacaoId = transacao.Id,
                    Resumo = "Pix (Mercado Pago)",
                    AprovadoEm = agora
                };
                pedido.StatusAtual = PedidoEtapa.Pagamento;
                pedido.Timeline.Add(new PedidoTimelineEvento
                {
                    Id = Guid.NewGuid(),
                    PedidoId = pedido.Id,
                    Etapa = PedidoEtapa.Pagamento,
                    Em = agora,
                    Por = PedidoActor.Cliente,
                    Observacao = $"Pagamento aprovado via {origem}"
                });
            }

            await transacoes.SaveChangesAsync(ct);
            await pedidos.SaveChangesAsync(ct);
        }
        else
        {
            await transacoes.SaveChangesAsync(ct);
        }

        logger.LogInformation("Transação {TransacaoId} atualizada para {Status} via {Origem}.",
            transacao.Id, novoStatus, origem);
        return true;
    }
}