using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GatumsGourmet.Application.UseCases.V1.Manutencao.PurgeExpiredRefreshTokens;

public sealed class PurgeExpiredRefreshTokensHandler(
    IRefreshTokenStore store,
    IClock clock,
    IConfiguration config) : IPurgeExpiredRefreshTokensHandler
{
    public async Task<Result<int>> Handle(CancellationToken ct = default)
    {
        var retentionDays = config.GetValue<int?>("Auth:RefreshTokenRetentionDays") ?? 30;
        var cutoff = clock.UtcNow.AddDays(-retentionDays);
        var removed = await store.DeleteExpiredAsync(cutoff, ct);
        return Result<int>.Ok(removed);
    }
}
