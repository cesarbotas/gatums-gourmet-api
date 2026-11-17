using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;

public interface IProcessarCartaoDebitoHandler
{
    Task<Result<TransacaoResponse>> Handle(ProcessarCartaoDebitoRequest request, CancellationToken ct = default);
}
