using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;

public record IniciarPixRequest(
    Guid PedidoId,
    decimal? Valor = null,
    string? PayerEmail = null,
    string? PayerNome = null);

public class IniciarPixValidator : AbstractValidator<IniciarPixRequest>
{
    public IniciarPixValidator()
    {
        RuleFor(x => x.PedidoId).NotEmpty().WithMessage("PedidoId é obrigatório.");
        When(x => x.Valor.HasValue, () =>
        {
            RuleFor(x => x.Valor!.Value).GreaterThan(0).WithMessage("Valor deve ser maior que zero.");
        });
        When(x => !string.IsNullOrWhiteSpace(x.PayerEmail), () =>
        {
            RuleFor(x => x.PayerEmail!).EmailAddress().WithMessage("E-mail do pagador inválido.");
        });
    }
}

public interface IIniciarPixHandler
{
    Task<Result<TransacaoResponse>> Handle(IniciarPixRequest request, string? idempotencyKey, CancellationToken ct = default);
}

public sealed class IniciarPixHandler(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IPixProvider pixProvider,
    IClock clock,
    ILogger<IniciarPixHandler> logger) : IIniciarPixHandler
{
    public async Task<Result<TransacaoResponse>> Handle(IniciarPixRequest request, string? idempotencyKey, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(request.PedidoId, ct);
        if (pedido is null) return Result<TransacaoResponse>.Fail("Pedido não encontrado.");

        var valor = request.Valor is > 0 ? request.Valor.Value : pedido.Total;

        // Se já existe transação pendente para este pedido, reaproveita se for Pix
        var existing = await transacoes.GetByPedidoIdAsync(request.PedidoId, ct);
        if (existing is not null && existing.Status == StatusTransacao.Pendente && existing.Metodo == MetodoPagamento.Pix)
        {
            logger.LogInformation("Reaproveitando transação PIX pendente existente. TransacaoId={Id}", existing.Id);
            return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(existing));
        }

        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = request.PedidoId,
            Metodo = MetodoPagamento.Pix,
            Valor = valor,
            Status = StatusTransacao.Pendente,
            Operadora = "pix",
            CriadoEm = clock.UtcNow,
            AtualizadoEm = clock.UtcNow
        };

        var payerNome = !string.IsNullOrWhiteSpace(request.PayerNome) ? request.PayerNome : pedido.ClienteNome;
        var payerEmail = request.PayerEmail;
        var descricao = $"Pedido Gatums #{pedido.Id.ToString()[..8]}";

        var pix = await pixProvider.GerarAsync(transacao.Id, valor, descricao, payerEmail, payerNome, ct);
        transacao.PixCopiaCola = pix.CopiaCola;
        transacao.PixExpiraEm = pix.ExpiraEm;
        transacao.PixQrCodeBase64 = pix.QrCodeBase64;

        if (!string.IsNullOrWhiteSpace(pix.OperadoraRef))
        {
            transacao.OperadoraRef = pix.OperadoraRef;
            transacao.Operadora = "mercadopago";
        }

        await transacoes.AddAsync(transacao, ct);
        await transacoes.SaveChangesAsync(ct);

        logger.LogInformation("Pagamento PIX iniciado com sucesso. TransacaoId={Id} PedidoId={PedidoId} Valor={Valor} Operadora={Operadora}",
            transacao.Id, request.PedidoId, valor, transacao.Operadora);

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}

