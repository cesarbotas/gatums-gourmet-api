using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.Pagamentos.IniciarPagamento;

public record IniciarPagamentoRequest(Guid PedidoId, MetodoPagamento Metodo, decimal Valor);

public class IniciarPagamentoValidator : AbstractValidator<IniciarPagamentoRequest>
{
    public IniciarPagamentoValidator()
    {
        RuleFor(x => x.PedidoId).NotEmpty();
        RuleFor(x => x.Valor).GreaterThan(0);
    }
}

public interface IIniciarPagamentoHandler
{
    Task<Result<TransacaoResponse>> Handle(IniciarPagamentoRequest request, string? idempotencyKey, CancellationToken ct = default);
}

public sealed class IniciarPagamentoHandler(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IPixProvider pixProvider,
    IClock clock,
    ILogger<IniciarPagamentoHandler> logger) : IIniciarPagamentoHandler
{
    public async Task<Result<TransacaoResponse>> Handle(IniciarPagamentoRequest request, string? idempotencyKey, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(request.PedidoId, ct);
        if (pedido is null) return Result<TransacaoResponse>.Fail("Pedido não encontrado.");

        var existing = await transacoes.GetByPedidoIdAsync(request.PedidoId, ct);
        if (existing is not null && existing.Status == StatusTransacao.Pendente)
            return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(existing));

        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = request.PedidoId,
            Metodo = request.Metodo,
            Valor = request.Valor,
            Status = StatusTransacao.Pendente,
            Operadora = request.Metodo == MetodoPagamento.Pix ? "pix" : "mock_gateway",
            CriadoEm = clock.UtcNow,
            AtualizadoEm = clock.UtcNow
        };

        if (request.Metodo == MetodoPagamento.Pix)
        {
            var pix = await pixProvider.GerarAsync(transacao.Id, request.Valor, ct);
            transacao.PixCopiaCola = pix.CopiaCola;
            transacao.PixExpiraEm = pix.ExpiraEm;
        }

        await transacoes.AddAsync(transacao, ct);
        await transacoes.SaveChangesAsync(ct);

        logger.LogInformation("Pagamento iniciado. TransacaoId={Id} Metodo={Metodo}", transacao.Id, request.Metodo);
        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}
