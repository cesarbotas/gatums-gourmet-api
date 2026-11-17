using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ValidarPagamentoParaAprovacao;

public interface IValidarPagamentoParaAprovacaoHandler
{
    Task<Result<ValidarPagamentoParaAprovacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default);
}
