using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoCredito;

public interface IProcessarCartaoCreditoHandler
{
    Task<Result<TransacaoResponse>> Handle(ProcessarCartaoCreditoRequest request, CancellationToken ct = default);
}
