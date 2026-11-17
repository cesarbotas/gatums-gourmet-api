using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;

public interface IProcessarPagamentoCartaoHandler
{
    Task<Result<TransacaoResponse>> Handle(Guid transacaoId,
        ProcessarPagamentoCartaoRequest request,
        CancellationToken ct = default);
}