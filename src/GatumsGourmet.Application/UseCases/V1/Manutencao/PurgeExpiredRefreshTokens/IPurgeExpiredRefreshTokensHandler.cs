using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Manutencao.PurgeExpiredRefreshTokens;

public interface IPurgeExpiredRefreshTokensHandler
{
    Task<Result<int>> Handle(CancellationToken ct = default);
}
