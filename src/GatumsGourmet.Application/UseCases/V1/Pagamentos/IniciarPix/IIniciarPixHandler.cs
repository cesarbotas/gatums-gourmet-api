using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;

public interface IIniciarPixHandler
{
    Task<Result<TransacaoResponse>> Handle(
        IniciarPixRequest request,
        string? idempotencyKey,
        CancellationToken ct = default
    );
}