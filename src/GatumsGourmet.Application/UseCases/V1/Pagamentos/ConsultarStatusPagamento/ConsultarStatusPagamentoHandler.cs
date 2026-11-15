using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ConsultarStatusPagamento;

public interface IConsultarStatusPagamentoHandler
{
    Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default);
}

public sealed class ConsultarStatusPagamentoHandler : IConsultarStatusPagamentoHandler
{
    private readonly ITransacaoRepository _transacoes;
    private readonly IPedidoRepository? _pedidos;
    private readonly IPixProvider? _pixProvider;
    private readonly IClock? _clock;

    public ConsultarStatusPagamentoHandler(ITransacaoRepository transacoes)
        : this(transacoes, null, null, null)
    {
    }

    public ConsultarStatusPagamentoHandler(
        ITransacaoRepository transacoes,
        IPedidoRepository? pedidos,
        IPixProvider? pixProvider,
        IClock? clock)
    {
        _transacoes = transacoes;
        _pedidos = pedidos;
        _pixProvider = pixProvider;
        _clock = clock;
    }

    public async Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default)
    {
        var transacao = await _transacoes.GetByIdAsync(transacaoId, ct);
        if (transacao is null) return Result<TransacaoResponse>.Fail("Transação não encontrada.");

        // Sincronização automática com provedor externo (ex.: Mercado Pago) se ainda estiver pendente
        if (transacao.Status == StatusTransacao.Pendente
            && transacao.Metodo == MetodoPagamento.Pix
            && !string.IsNullOrWhiteSpace(transacao.OperadoraRef)
            && _pixProvider is not null
            && _pedidos is not null
            && _clock is not null)
        {
            var statusMp = await _pixProvider.ConsultarStatusAsync(transacao.OperadoraRef, ct);
            if (string.Equals(statusMp, "approved", StringComparison.OrdinalIgnoreCase))
            {
                transacao.Status = StatusTransacao.Aprovado;
                transacao.AtualizadoEm = _clock.UtcNow;

                var pedido = await _pedidos.GetByIdAsync(transacao.PedidoId, ct);
                if (pedido is not null && pedido.StatusAtual is PedidoEtapa.Criado or PedidoEtapa.Aprovado)
                {
                    pedido.Pagamento = new PedidoPagamento
                    {
                        PedidoId = pedido.Id,
                        TransacaoId = transacao.Id,
                        Resumo = "Pix (Mercado Pago)",
                        AprovadoEm = _clock.UtcNow
                    };
                    pedido.StatusAtual = PedidoEtapa.Pagamento;
                    pedido.Timeline.Add(new PedidoTimelineEvento
                    {
                        Id = Guid.NewGuid(),
                        PedidoId = pedido.Id,
                        Etapa = PedidoEtapa.Pagamento,
                        Em = _clock.UtcNow,
                        Por = PedidoActor.Cliente,
                        Observacao = "Pagamento aprovado na consulta de status"
                    });
                }

                await _transacoes.SaveChangesAsync(ct);
                await _pedidos.SaveChangesAsync(ct);
            }
        }

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}
