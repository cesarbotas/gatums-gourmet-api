using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Application.UseCases.V1.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.ConfirmarPagamentoPresencialCartao;

public interface IConfirmarPagamentoPresencialCartaoHandler
{
    Task<Result<TransacaoResponse>> Handle(
        Guid transacaoId,
        ConfirmarPagamentoPresencialCartaoRequest request,
        CancellationToken ct = default);
}
