using GatumsGourmet.Application.UseCases.V1.Pagamentos;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.IniciarPix;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.RegistrarPagamentoPresencial;

/// <summary>
/// Registra o pagamento de uma venda presencial. Comportamento por forma:
/// - Dinheiro: aprovado imediatamente, calcula troco.
/// - Cartão: aguarda confirmação manual do operador.
/// - Pix: gera QR Code via <strong>Mercado Pago Orders API</strong> (IPixOrdersProvider)
///         para ser consistente com o fluxo V2 do site.
/// </summary>
public sealed class RegistrarPagamentoPresencialHandler(
    IPedidoRepository pedidos,
    ITransacaoRepository transacoes,
    IUsuarioRepository usuarios,
    IPixOrdersProvider pixOrdersProvider,
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

        if (pedido.Canal != CanalVenda.Presencial)
            return Result<TransacaoResponse>.Fail("Este pedido não é uma venda presencial.");

        if (request.Metodo == MetodoPagamentoPresencial.Dinheiro)
        {
            if (request.ValorRecebido is null or <= 0)
                return Result<TransacaoResponse>.Fail("ValorRecebido é obrigatório para pagamento em dinheiro.");

            if (request.ValorRecebido < pedido.Total)
                return Result<TransacaoResponse>.Fail(
                    $"Valor recebido ({request.ValorRecebido:F2}) é menor que o total ({pedido.Total:F2}).");
        }

        // Pix via Orders exige e-mail do pagador — resolve do request, do pedido ou do usuário
        if (request.Metodo == MetodoPagamentoPresencial.Pix)
        {
            var email = request.PayerEmail;
            if (string.IsNullOrWhiteSpace(email))
            {
                var usuario = await usuarios.GetByIdAsync(pedido.UsuarioId, ct);
                email = usuario?.Email;
            }
            if (string.IsNullOrWhiteSpace(email))
                return Result<TransacaoResponse>.Fail(
                    "E-mail do pagador é obrigatório para gerar o Pix. Informe 'payerEmail' no body.");
        }

        var metodoDominio = request.Metodo switch
        {
            MetodoPagamentoPresencial.Dinheiro     => MetodoPagamento.CartaoDebito, // placeholder (Dinheiro não tem enum online)
            MetodoPagamentoPresencial.CartaoDebito  => MetodoPagamento.CartaoDebito,
            MetodoPagamentoPresencial.CartaoCredito => MetodoPagamento.CartaoCredito,
            MetodoPagamentoPresencial.Pix           => MetodoPagamento.Pix,
            _                                       => MetodoPagamento.CartaoDebito
        };

        var agora = clock.UtcNow;
        var transacao = new TransacaoPagamento
        {
            Id          = Guid.NewGuid(),
            PedidoId    = request.PedidoId,
            Metodo      = metodoDominio,
            Valor       = pedido.Total,
            CriadoEm    = agora,
            AtualizadoEm = agora
        };

        switch (request.Metodo)
        {
            // ── Dinheiro: aprovação imediata, troco calculado ──
            case MetodoPagamentoPresencial.Dinheiro:
                transacao.Status        = StatusTransacao.Aprovado;
                transacao.Operadora     = "dinheiro";
                transacao.ValorRecebido = request.ValorRecebido;
                transacao.Troco         = Math.Round(request.ValorRecebido!.Value - pedido.Total, 2);
                AprovacaoAutomatica(pedido, transacao, "Dinheiro (venda presencial)", agora);
                break;

            // ── Cartão: aguarda confirmação manual do operador ─
            case MetodoPagamentoPresencial.CartaoDebito:
            case MetodoPagamentoPresencial.CartaoCredito:
                transacao.Status    = StatusTransacao.Pendente;
                transacao.Operadora = "maquininha";
                break;

            // ── Pix: QR Code via Mercado Pago Orders API ───────
            case MetodoPagamentoPresencial.Pix:
                var payerEmail = !string.IsNullOrWhiteSpace(request.PayerEmail)
                    ? request.PayerEmail
                    : (await usuarios.GetByIdAsync(pedido.UsuarioId, ct))?.Email ?? string.Empty;

                var descricao = $"Pedido Gatums #{pedido.Id.ToString()[..8]}";
                PixOrderResult order;
                try
                {
                    order = await pixOrdersProvider.CriarOrderAsync(
                        new PixOrderRequest(transacao.Id, pedido.Total, descricao, payerEmail, pedido.ClienteNome), ct);
                }
                catch (PixOrdersProviderException ex)
                {
                    logger.LogWarning(ex, "Falha ao criar order Pix presencial. PedidoId={PedidoId} StatusCode={Code}",
                        request.PedidoId, ex.StatusCode);
                    return Result<TransacaoResponse>.Fail(ex.Message);
                }

                transacao.Status         = StatusTransacao.Pendente;
                transacao.Operadora      = IniciarPixV2Handler.OperadoraOrders; // "mercadopago-orders"
                transacao.OperadoraRef   = order.OrderId;
                transacao.PixCopiaCola   = order.CopiaCola;
                transacao.PixQrCodeBase64 = order.QrCodeBase64;
                transacao.PixExpiraEm    = order.ExpiraEm;

                logger.LogInformation(
                    "Pix presencial via Orders API. TransacaoId={Id} OrderId={OrderId} PedidoId={PedidoId}",
                    transacao.Id, order.OrderId, request.PedidoId);
                break;
        }

        await transacoes.AddAsync(transacao, ct);
        await transacoes.SaveChangesAsync(ct);
        await pedidos.SaveChangesAsync(ct);

        logger.LogInformation(
            "Pagamento presencial registrado. TransacaoId={Id} Metodo={Metodo} Operadora={Operadora} PedidoId={PedidoId}",
            transacao.Id, request.Metodo, transacao.Operadora, request.PedidoId);

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }

    private static void AprovacaoAutomatica(Pedido pedido, TransacaoPagamento transacao, string resumo, DateTime agora)
    {
        pedido.StatusAtual = PedidoEtapa.PagamentoAprovado;
        pedido.Pagamento   = new PedidoPagamento
        {
            PedidoId     = pedido.Id,
            TransacaoId  = transacao.Id,
            Resumo       = resumo,
            AprovadoEm   = agora
        };
        pedido.Timeline.Add(new PedidoTimelineEvento
        {
            Id          = Guid.NewGuid(),
            PedidoId    = pedido.Id,
            Etapa       = PedidoEtapa.PagamentoAprovado,
            Em          = agora,
            Por         = PedidoActor.Admin,
            Observacao  = "Pagamento em dinheiro aprovado automaticamente"
        });
    }
}
