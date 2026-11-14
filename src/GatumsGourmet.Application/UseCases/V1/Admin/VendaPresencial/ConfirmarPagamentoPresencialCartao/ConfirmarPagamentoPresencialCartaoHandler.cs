using GatumsGourmet.Application.UseCases.V1.Pagamentos;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.ConfirmarPagamentoPresencialCartao;

/// <summary>
/// Operador confirma (ou recusa) o pagamento via maquininha. Ao aprovar,
/// avança o pedido para <see cref="PedidoEtapa.PagamentoAprovado"/> automaticamente.
/// </summary>
public sealed class ConfirmarPagamentoPresencialCartaoHandler(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IClock clock,
    ILogger<ConfirmarPagamentoPresencialCartaoHandler> logger) : IConfirmarPagamentoPresencialCartaoHandler
{
    public async Task<Result<TransacaoResponse>> Handle(
        Guid transacaoId,
        ConfirmarPagamentoPresencialCartaoRequest request,
        CancellationToken ct = default)
    {
        var transacao = await transacoes.GetByIdAsync(transacaoId, ct);
        if (transacao is null)
            return Result<TransacaoResponse>.Fail("Transação não encontrada.");

        if (transacao.Status != StatusTransacao.Pendente)
            return Result<TransacaoResponse>.Fail("Transação já foi processada.");

        if (transacao.Metodo is not (MetodoPagamento.CartaoCredito or MetodoPagamento.CartaoDebito))
            return Result<TransacaoResponse>.Fail("Esta confirmação é exclusiva para pagamentos com cartão.");

        var agora = clock.UtcNow;
        transacao.AtualizadoEm = agora;

        if (request.Aprovado)
        {
            transacao.Status = StatusTransacao.Aprovado;

            var pedido = await pedidos.GetByIdAsync(transacao.PedidoId, ct);
            if (pedido is not null)
            {
                pedido.StatusAtual = PedidoEtapa.PagamentoAprovado;
                pedido.Pagamento = new PedidoPagamento
                {
                    PedidoId = pedido.Id,
                    TransacaoId = transacao.Id,
                    Resumo = $"Cartão (maquininha) — {request.Observacao ?? "aprovado"}",
                    AprovadoEm = agora
                };
                pedido.Timeline.Add(new PedidoTimelineEvento
                {
                    Id = Guid.NewGuid(),
                    PedidoId = pedido.Id,
                    Etapa = PedidoEtapa.PagamentoAprovado,
                    Em = agora,
                    Por = PedidoActor.Admin,
                    Observacao = request.Observacao ?? "Cartão aprovado pelo operador"
                });
                await pedidos.SaveChangesAsync(ct);
            }

            logger.LogInformation(
                "Pagamento presencial cartão APROVADO. TransacaoId={Id} Obs={Obs}",
                transacaoId, request.Observacao);
        }
        else
        {
            transacao.Status = StatusTransacao.Recusado;
            transacao.MensagemErro = request.Observacao ?? "Recusado pelo operador";

            logger.LogWarning(
                "Pagamento presencial cartão RECUSADO. TransacaoId={Id} Obs={Obs}",
                transacaoId, request.Observacao);
        }

        await transacoes.SaveChangesAsync(ct);
        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}
