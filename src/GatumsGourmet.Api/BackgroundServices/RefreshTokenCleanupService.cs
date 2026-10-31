using GatumsGourmet.Application.UseCases.V1.Manutencao.PurgeExpiredRefreshTokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Api.BackgroundServices;

/// <summary>
/// Limpeza periódica da tabela RefreshTokens. Resolve o caso de uso
/// <see cref="IPurgeExpiredRefreshTokensHandler"/> (Scoped) em um escopo próprio via
/// <see cref="IServiceScopeFactory"/> — o handler carrega toda a lógica; este serviço só agenda.
/// </summary>
public sealed class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var horas = config.GetValue<int?>("Auth:CleanupIntervalHours") ?? 12;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(horas <= 0 ? 12 : horas));
        // roda uma vez no start e depois a cada intervalo
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IPurgeExpiredRefreshTokensHandler>();
                var result = await handler.Handle(stoppingToken);
                if (result.IsSuccess)
                    logger.LogInformation("Limpeza de refresh tokens: {Qtd} removidos.", result.Data);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na limpeza periódica de refresh tokens.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
