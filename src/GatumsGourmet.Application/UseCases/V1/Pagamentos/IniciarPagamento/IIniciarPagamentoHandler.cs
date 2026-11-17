using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;

public interface IIniciarPagamentoHandler
{
    Task<Result<TransacaoResponse>> Handle(IniciarPagamentoRequest request, string? idempotencyKey, CancellationToken ct = default);
}
