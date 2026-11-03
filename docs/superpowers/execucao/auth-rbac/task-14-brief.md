# Task 14: Housekeeping — PurgeExpiredRefreshTokens + BackgroundService + endpoint manual

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 140 unit + 95 integração verdes.

**Objetivo:** limpeza periódica da tabela RefreshTokens — um caso de uso dedicado, um BackgroundService (L1) e um endpoint manual (L3), ambos usando o mesmo use case.

## Constraints globais
- net10.0, noBOM. Caso de uso em `Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/`. BackgroundService em `Api/BackgroundServices/RefreshTokenCleanupService.cs`. Endpoint manual em AuthEndpoints (grupo /auth).
- Interface disponível (Tasks 2/5): `IRefreshTokenStore.DeleteExpiredAsync(DateTime cutoff, ct) → int`. `IClock.UtcNow`.
- Config: `Auth:RefreshTokenRetentionDays` (default 30), `Auth:CleanupIntervalHours` (default 12).

## 1. Use case `Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/`
- `IPurgeExpiredRefreshTokensHandler { Task<Result<int>> Handle(CancellationToken ct = default); }`
- `PurgeExpiredRefreshTokensHandler` — deps: `IRefreshTokenStore store, IClock clock, IConfiguration config`.
  - `retentionDays = config.GetValue<int?>("Auth:RefreshTokenRetentionDays") ?? 30;`
  - `cutoff = clock.UtcNow.AddDays(-retentionDays);`
  - `removed = await store.DeleteExpiredAsync(cutoff, ct);`
  - retorna `Result<int>.Ok(removed)`.
  (Sem Request — não há parâmetros de entrada além do cutoff derivado da config.)
- Registrar no DI (`ApplicationServiceCollectionExtensions`): `IPurgeExpiredRefreshTokensHandler→PurgeExpiredRefreshTokensHandler`.

## 2. BackgroundService `Api/BackgroundServices/RefreshTokenCleanupService.cs`
```csharp
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
```
(usings: Microsoft.Extensions.Hosting, DependencyInjection, Logging, Configuration; GatumsGourmet.Application.UseCases.V1.Manutencao.PurgeExpiredRefreshTokens.)
Registrar no `Program.cs`: `builder.Services.AddHostedService<RefreshTokenCleanupService>();` (após AddApplication/AddInfrastructure). Adicionar `using GatumsGourmet.Api.BackgroundServices;`.

## 3. Endpoint manual (AuthEndpoints.cs, grupo /auth)
`POST /maintenance/purge-tokens` com `RequirePermission("usuarios:gerenciar")`:
```csharp
group.MapPost("/maintenance/purge-tokens", async (IPurgeExpiredRefreshTokensHandler handler, CancellationToken ct) =>
    (await handler.Handle(ct)).ToHttpResult())
    .RequirePermission("usuarios:gerenciar");
```
(Adicionar usings: `GatumsGourmet.Api.Authorization;` e `GatumsGourmet.Application.UseCases.V1.Manutencao.PurgeExpiredRefreshTokens;`.)

## 4. appsettings.json
Adicionar uma seção `Auth`:
```json
"Auth": {
  "RefreshTokenRetentionDays": 30,
  "CleanupIntervalHours": 12
}
```
E, se existir `Jwt:AccessTokenMinutes`/`Jwt:RefreshTokenDays` ainda não declarados no appsettings.json, adicione-os à seção Jwt com os defaults (15 e 7) para ficarem documentados/configuráveis (eles hoje caem no default do código). Verifique a seção Jwt atual e complemente.

## Teste unitário (`tests/.../V1/Manutencao/PurgeExpiredRefreshTokensHandlerTests.cs`)
Mock de IRefreshTokenStore e IClock; IConfiguration InMemory com Auth:RefreshTokenRetentionDays=30.
- `Handle_RemoveExpirados_RetornaContagem`: clock fixo; verifica que `DeleteExpiredAsync` é chamado com `cutoff == now.AddDays(-30)` e que o Result traz a contagem retornada pelo store (ex.: 7).
- `Handle_RetentionConfiguravel`: com RetentionDays=10, cutoff == now.AddDays(-10).

(BackgroundService em si é difícil de testar unitariamente sem flakiness — NÃO exigir teste do service; o handler é o que carrega a lógica. Se quiser, um teste simples que o service resolve o handler via scope não agrega muito — pule.)

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test tests/GatumsGourmet.UnitTests` verde (reporte contagem). O hosted service subindo não deve quebrar os testes de integração (ele roda em background; o DeleteExpired é idempotente). Rode a integração e confirme que continua verde.

## Commit
`feat: housekeeping de refresh tokens (background + endpoint manual)`

## Não dispare subagentes. O caso de uso é a fonte única da lógica; BackgroundService e endpoint apenas o invocam.
