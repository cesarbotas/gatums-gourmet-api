using GatumsGourmet.Application.UseCases.V1.Imagens.PurgeOrphanImagens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Api.BackgroundServices;

/// <summary>
/// Limpeza periódica de imagens órfãs no storage. Resolve o caso de uso
/// <see cref="IPurgeOrphanImagensHandler"/> (Scoped) em um escopo próprio via
/// <see cref="IServiceScopeFactory"/> — o handler carrega toda a lógica
/// (listar, cruzar com produtos, aplicar carência); este serviço só agenda.
/// Espelha o <see cref="RefreshTokenCleanupService"/>.
/// </summary>
public sealed class OrphanImageCleanupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<OrphanImageCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var horas = config.GetValue<int?>("Storage:OrphanCleanupIntervalHours") ?? 24;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(horas <= 0 ? 24 : horas));
        // roda uma vez no start e depois a cada intervalo
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IPurgeOrphanImagensHandler>();
                var result = await handler.Handle(stoppingToken);
                if (result.IsSuccess)
                    logger.LogInformation("Limpeza de imagens órfãs: {Qtd} removidas.", result.Data);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na limpeza periódica de imagens órfãs.");
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
