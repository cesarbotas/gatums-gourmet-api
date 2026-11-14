using GatumsGourmet.Application.UseCases.V1.Pagamentos;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.RegistrarPagamentoPresencial;

/// <summary>
/// Registra o pagamento de uma venda presencial. Comportamento por forma:
/// - Dinheiro: aprovado imediatamente, calcula troco.
/// - Cartão: aguarda confirmação manual do operador (§ConfirmarPagamentoPresencialCartao).
/// - Pix: gera QR Code e fica aguardando webhook/polling.
/// </summary>
public sealed class RegistrarPagamentoPresencialHandler(
    IPedidoRepository pedidos,
    ITransacaoRepository transacoes,
    IPixProvider pixProvider,
    IClock clock,
    ILogger<RegistrarPagamentoPresencialHandler> logger) : IRegistrarPagamentoPresencialHandler
{
    public async Task<Result<TransacaoResponse>> Handle(
        RegistrarPagamentoPresencialRequest request,
        CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(request.PedidoId, ct);
        if (pedido is null)
            return Result<TransacaoResponse>.Fail("Pedido não encontrado.");

        if (pedido.Canal != Domain.Enums.Pedidos.CanalVenda.Presencial)
            return Result<TransacaoResponse>.Fail("Este pedido não é uma venda presencial.");

        // Validação de dinheiro: valor recebido deve cobrir o total
        if (request.Metodo == MetodoPagamentoPresencial.Dinheiro)
        {
            if (request.ValorRecebido is null or <= 0)
                return Result<TransacaoResponse>.Fail("ValorRecebido é obrigatório para pagamento em dinheiro.");

            if (request.ValorRecebido < pedido.Total)
                return Result<TransacaoResponse>.Fail(
                    $"Valor recebido ({request.ValorRecebido:F2}) é menor que o total ({pedido.Total:F2}).");
        }

        // Mapeia método presencial para o enum de domínio
        var metodoDominio = request.Metodo switch
        {
            MetodoPagamentoPresencial.Dinheiro => MetodoPagamento.CartaoDebito, // placeholder; Dinheiro não existe no enum online
            MetodoPagamentoPresencial.CartaoDebito => MetodoPagamento.CartaoDebito,
            MetodoPagamentoPresencial.CartaoCredito => MetodoPagamento.CartaoCredito,
            MetodoPagamentoPresencial.Pix => MetodoPagamento.Pix,
            _ => MetodoPagamento.CartaoDebito
        };

        var agora = clock.UtcNow;
        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = request.PedidoId,
            Metodo = metodoDominio,
            Valor = pedido.Total,
            CriadoEm = agora,
            AtualizadoEm = agora
        };

        switch (request.Metodo)
        {
            // ── Dinheiro: aprovado imediatamente ───────────────
            case MetodoPagamentoPresencial.Dinheiro:
                var troco = request.ValorRecebido!.Value - pedido.Total;
                transacao.Status = StatusTransacao.Aprovado;
                transacao.Operadora = "dinheiro";
                transacao.ValorRecebido = request.ValorRecebido;
                transacao.Troco = Math.Round(troco, 2);
                AprovacaoAutomatica(pedido, transacao, agora);
                break;

            // ── Cartão: aguarda confirmação do operador ────────
            case MetodoPagamentoPresencial.CartaoDebito:
            case MetodoPagamentoPresencial.CartaoCredito:
                transacao.Status = StatusTransacao.Pendente;
                transacao.Operadora = "maquininha";
                break;

            // ── Pix: gera QR Code ──────────────────────────────
            case MetodoPagamentoPresencial.Pix:
                transacao.Status = StatusTransacao.Pendente;
                var descricao = $"Pedido Gatums #{pedido.Id.ToString()[..8]}";
                var pix = await pixProvider.GerarAsync(
                    transacao.Id, pedido.Total, descricao, request.PayerEmail, pedido.ClienteNome, ct);
                transacao.PixCopiaCola = pix.CopiaCola;
                transacao.PixExpiraEm = pix.ExpiraEm;
                transacao.PixQrCodeBase64 = pix.QrCodeBase64;
                transacao.Operadora = !string.IsNullOrWhiteSpace(pix.OperadoraRef) ? "mercadopago" : "pix";
                if (!string.IsNullOrWhiteSpace(pix.OperadoraRef))
                    transacao.OperadoraRef = pix.OperadoraRef;
                break;
        }

        await transacoes.AddAsync(transacao, ct);
        await transacoes.SaveChangesAsync(ct);
        await pedidos.SaveChangesAsync(ct);

        logger.LogInformation(
            "Pagamento presencial registrado. TransacaoId={Id} Metodo={Metodo} Status={Status} PedidoId={PedidoId}",
            transacao.Id, request.Metodo, transacao.Status, request.PedidoId);

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }

    private static void AprovacaoAutomatica(Pedido pedido, TransacaoPagamento transacao, DateTime agora)
    {
        pedido.StatusAtual = PedidoEtapa.PagamentoAprovado;
        pedido.Pagamento = new PedidoPagamento
        {
            PedidoId = pedido.Id,
            TransacaoId = transacao.Id,
            Resumo = "Dinheiro (venda presencial)",
            AprovadoEm = agora
        };
        pedido.Timeline.Add(new PedidoTimelineEvento
        {
            Id = Guid.NewGuid(),
            PedidoId = pedido.Id,
            Etapa = PedidoEtapa.PagamentoAprovado,
            Em = agora,
            Por = PedidoActor.Admin,
            Observacao = "Pagamento em dinheiro aprovado automaticamente"
        });
    }
}
