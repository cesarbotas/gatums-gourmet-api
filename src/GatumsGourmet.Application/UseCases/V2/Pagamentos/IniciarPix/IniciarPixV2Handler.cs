using GatumsGourmet.Application.UseCases.V1.Pagamentos;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V2.Pagamentos.IniciarPix;

public interface IIniciarPixV2Handler
{
    Task<Result<TransacaoResponse>> Handle(IniciarPixRequest request, string? idempotencyKey, CancellationToken ct = default);
}

public sealed class IniciarPixV2Handler(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IUsuarioRepository usuarios,
    IPixOrdersProvider provider,
    IClock clock,
    ILogger<IniciarPixV2Handler> logger) : IIniciarPixV2Handler
{
    public const string OperadoraOrders = "mercadopago-orders";

    public async Task<Result<TransacaoResponse>> Handle(IniciarPixRequest request, string? idempotencyKey, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(request.PedidoId, ct);
        if (pedido is null) return Result<TransacaoResponse>.Fail("Pedido não encontrado.");

        // Reaproveita somente transação Pix pendente criada via Orders API (V2); transações V1 são ignoradas.
        var existing = await transacoes.GetByPedidoIdAsync(request.PedidoId, ct);
        if (existing is not null
            && existing.Status == StatusTransacao.Pendente
            && existing.Metodo == MetodoPagamento.Pix
            && existing.Operadora == OperadoraOrders)
        {
            logger.LogInformation("Reaproveitando transação PIX (Orders) pendente existente. TransacaoId={Id}", existing.Id);
            return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(existing));
        }

        var payerEmail = request.PayerEmail;
        if (string.IsNullOrWhiteSpace(payerEmail))
        {
            var usuario = await usuarios.GetByIdAsync(pedido.UsuarioId, ct);
            payerEmail = usuario?.Email;
        }

        if (string.IsNullOrWhiteSpace(payerEmail))
        {
            logger.LogWarning("PIX V2 não iniciado: sem e-mail do pagador. PedidoId={PedidoId}", request.PedidoId);
            return Result<TransacaoResponse>.Fail("E-mail do pagador é obrigatório para gerar o Pix.");
        }

        var valor = request.Valor is > 0 ? request.Valor.Value : pedido.Total;
        var payerNome = !string.IsNullOrWhiteSpace(request.PayerNome) ? request.PayerNome : pedido.ClienteNome;
        var descricao = $"Pedido Gatums #{pedido.Id.ToString()[..8]}";

        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = request.PedidoId,
            Metodo = MetodoPagamento.Pix,
            Valor = valor,
            Status = StatusTransacao.Pendente,
            Operadora = OperadoraOrders,
            CriadoEm = clock.UtcNow,
            AtualizadoEm = clock.UtcNow
        };

        PixOrderResult order;
        try
        {
            order = await provider.CriarOrderAsync(
                new PixOrderRequest(transacao.Id, valor, descricao, payerEmail.Trim(), payerNome), ct);
        }
        catch (PixOrdersProviderException ex)
        {
            logger.LogWarning(ex, "Falha ao criar order Pix. PedidoId={PedidoId} StatusCode={StatusCode}", request.PedidoId, ex.StatusCode);
            return Result<TransacaoResponse>.Fail(ex.Message);
        }

        transacao.OperadoraRef = order.OrderId;
        transacao.PixCopiaCola = order.CopiaCola;
        transacao.PixQrCodeBase64 = order.QrCodeBase64;
        transacao.PixExpiraEm = order.ExpiraEm;

        await transacoes.AddAsync(transacao, ct);
        await transacoes.SaveChangesAsync(ct);

        logger.LogInformation("Pagamento PIX (Orders) iniciado. TransacaoId={Id} PedidoId={PedidoId} Valor={Valor} OrderId={OrderId}",
            transacao.Id, request.PedidoId, valor, order.OrderId);

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}
