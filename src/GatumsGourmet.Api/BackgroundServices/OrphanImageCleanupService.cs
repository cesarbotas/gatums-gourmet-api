using GatumsGourmet.Application.UseCases.V1.Imagens.PurgeOrphanImagens;
using GatumsGourmet.Api.Extensions;
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
        var cleanupEnabled = config.GetValue<bool?>("Storage:OrphanCleanupEnabled") ?? true;
        if (!cleanupEnabled)
        {
            logger.LogInformation("Limpeza de imagens órfãs desabilitada (Storage:OrphanCleanupEnabled=false).");
            return;
        }

        var horas = config.GetValue<int?>("Storage:OrphanCleanupIntervalHours") ?? 24;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(horas <= 0 ? 24 : horas));
        // roda uma vez no start e depois a cada intervalo
        do
        {
            try
            {
                logger.LogInformation("Iniciando limpeza de imagens órfãs...");
                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IPurgeOrphanImagensHandler>();
                var result = await handler.Handle(stoppingToken);

                if (result.IsSuccess)
                    logger.LogInformation("Limpeza de imagens órfãs concluída: {Qtd} removidas.", result.Data);
                else
                    logger.LogWarning("Limpeza de imagens órfãs falhou sem exceção: {Error}", result.Error);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                // Deep exception logging: percorre InnerException e AggregateException.
                logger.LogDeep(ex, "Falha na limpeza periódica de imagens órfãs. ExceptionType={ExceptionType}",
                    ex.GetType().Name);
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
