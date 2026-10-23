using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos.ConcluirPagamentoPedido;

public record ConcluirPagamentoPedidoRequest(Guid TransacaoId);

public interface IConcluirPagamentoPedidoHandler
{
    Task<Result<bool>> Handle(Guid pedidoId, ConcluirPagamentoPedidoRequest request, CancellationToken ct = default);
}

public sealed class ConcluirPagamentoPedidoHandler(
    IPedidoRepository pedidos,
    ITransacaoRepository transacoes,
    IClock clock,
    ILogger<ConcluirPagamentoPedidoHandler> logger) : IConcluirPagamentoPedidoHandler
{
    public async Task<Result<bool>> Handle(Guid pedidoId, ConcluirPagamentoPedidoRequest request, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(pedidoId, ct);
        if (pedido is null) return Result<bool>.Fail("Pedido não encontrado.");

        var transacao = await transacoes.GetByIdAsync(request.TransacaoId, ct);
        if (transacao is null || transacao.PedidoId != pedidoId) return Result<bool>.Fail("Transação inválida.");
        if (transacao.Status != StatusTransacao.Aprovado) return Result<bool>.Fail("Transação não aprovada.");

        var resumo = transacao.Metodo == MetodoPagamento.Pix ? "Pix" : $"{transacao.CartaoBandeira} **** {transacao.CartaoUltimos4}";

        pedido.Pagamento = new PedidoPagamento { PedidoId = pedidoId, TransacaoId = transacao.Id, Resumo = resumo, AprovadoEm = clock.UtcNow };
        pedido.StatusAtual = PedidoEtapa.Pagamento;
        pedido.Timeline.Add(new PedidoTimelineEvento { Id = Guid.NewGuid(), PedidoId = pedido.Id, Etapa = PedidoEtapa.Pagamento, Em = clock.UtcNow, Por = PedidoActor.Cliente });

        await pedidos.SaveChangesAsync(ct);
        logger.LogInformation("Pagamento concluído. PedidoId={PedidoId} TransacaoId={TransacaoId}", pedidoId, request.TransacaoId);
        return Result<bool>.Ok(true);
    }
}

