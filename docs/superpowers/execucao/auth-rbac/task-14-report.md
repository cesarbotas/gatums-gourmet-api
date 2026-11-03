# Task 14 — Report: Housekeeping de refresh tokens

**Branch:** feature/AjusteLoginToken
**Status:** Concluída ✅

## Resumo
Limpeza periódica da tabela `RefreshTokens` implementada como um caso de uso dedicado
(fonte única da lógica), invocado por dois gatilhos: um `BackgroundService` (L1, agendado)
e um endpoint manual (L3, protegido por permissão). Nenhuma lógica duplicada.

## Use case — `Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/`
- `IPurgeExpiredRefreshTokensHandler { Task<Result<int>> Handle(CancellationToken ct = default); }`
- `PurgeExpiredRefreshTokensHandler(IRefreshTokenStore store, IClock clock, IConfiguration config)`:
  - `retentionDays = config.GetValue<int?>("Auth:RefreshTokenRetentionDays") ?? 30;`
  - `cutoff = clock.UtcNow.AddDays(-retentionDays);`
  - `removed = await store.DeleteExpiredAsync(cutoff, ct);`
  - retorna `Result<int>.Ok(removed)`.
  - Sem Request (único parâmetro é o cutoff derivado da config).
- Registrado em `ApplicationServiceCollectionExtensions.AddApplication` como Scoped.

## BackgroundService — `Api/BackgroundServices/RefreshTokenCleanupService.cs`
- `sealed`, herda `BackgroundService`. Deps: `IServiceScopeFactory`, `IConfiguration`, `ILogger`.
- Resolve o handler **Scoped** num escopo próprio via `scopeFactory.CreateScope()` —
  não injeta o handler Scoped direto no Singleton.
- `PeriodicTimer` com intervalo `Auth:CleanupIntervalHours` (default 12; fallback para 12 se <= 0).
- Roda uma vez no start e depois a cada intervalo (`do { } while (await SafeWaitAsync(...))`).
- `OperationCanceledException` encerra sem ruído; demais exceções logam erro e seguem.
- Registrado no `Program.cs`: `builder.Services.AddHostedService<RefreshTokenCleanupService>();`
  (após AddApplication/AddInfrastructure/AddGatumsAuthentication/AddGatumsOpenApi) + `using GatumsGourmet.Api.BackgroundServices;`.

## Endpoint manual — `AuthEndpoints.cs` (grupo /auth)
- `POST /auth/maintenance/purge-tokens` → `RequirePermission("usuarios:gerenciar")`.
- Apenas invoca o handler: `(await handler.Handle(ct)).ToHttpResult()`.
- usings adicionados: `GatumsGourmet.Api.Authorization;` e
  `GatumsGourmet.Application.UseCases.V1.Manutencao.PurgeExpiredRefreshTokens;`.
- Permissão `usuarios:gerenciar` confirmada no seed da migration AuthRbacRefreshToken.

## Config — `appsettings.json`
- Nova seção `Auth`: `RefreshTokenRetentionDays=30`, `CleanupIntervalHours=12`.
- Seção `Jwt` complementada com `AccessTokenMinutes=15` e `RefreshTokenDays=7`
  (antes só caíam no default do código; agora documentados/configuráveis).

## Testes unitários — `tests/.../V1/Manutencao/PurgeExpiredRefreshTokensHandlerTests.cs`
Mock de `IRefreshTokenStore` + `IClock` (clock fixo 2026-06-15T12:00Z); `IConfiguration` InMemory.
- `Handle_RemoveExpirados_RetornaContagem`: RetentionDays=30 → `DeleteExpiredAsync` chamado com `cutoff == now.AddDays(-30)`; Result.Data == 7.
- `Handle_RetentionConfiguravel`: RetentionDays=10 → cutoff == now.AddDays(-10); Result.Data == 3.
- `Handle_SemConfig_UsaDefault30Dias`: sem chave → cutoff == now.AddDays(-30); Result.Data == 0.

BackgroundService não testado unitariamente (flaky, sem valor) — a lógica está toda no handler.

## Validação
- `dotnet build GatumsGourmet.sln`: **0 erros** (warnings pré-existentes, nenhum nos arquivos novos).
- `dotnet test tests/GatumsGourmet.UnitTests`: **143 testes verdes** (140 anteriores + 3 novos).
- `dotnet test tests/GatumsGourmet.IntegrationTests`: **95 testes verdes** — o hosted service
  subindo em background não quebrou nada (DeleteExpired é idempotente).

## Arquivos alterados
Modificados:
- `src/GatumsGourmet.Api/Endpoints/V1/AuthEndpoints.cs`
- `src/GatumsGourmet.Api/Program.cs`
- `src/GatumsGourmet.Api/appsettings.json`
- `src/GatumsGourmet.Application/ApplicationServiceCollectionExtensions.cs`

Novos:
- `src/GatumsGourmet.Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/IPurgeExpiredRefreshTokensHandler.cs`
- `src/GatumsGourmet.Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/PurgeExpiredRefreshTokensHandler.cs`
- `src/GatumsGourmet.Api/BackgroundServices/RefreshTokenCleanupService.cs`
- `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokensHandlerTests.cs`

## Auto-revisão
- Use case é a fonte única da lógica; BackgroundService e endpoint só invocam. ✅
- Scoped resolvido via escopo próprio (sem captive dependency no Singleton). ✅
- Config honrada com defaults seguros (30 dias / 12h; fallback de intervalo <= 0 → 12h). ✅
- Endpoint protegido por permissão existente no seed. ✅
- noBOM em todos os arquivos novos; estilo (primary constructors, sealed) alinhado ao repo. ✅

## Concerns
- `DeleteExpiredAsync` apaga tokens com `ExpiraEm < cutoff` **ou** revogados com `RevogadoEm < cutoff`
  (ver RefreshTokenStore) — ou seja, a retenção vale também para revogados antigos, o que é o
  comportamento desejado para housekeeping. Nenhum risco para auditoria de curto prazo.
- O hosted service roda uma purga logo no start da aplicação (inclusive em cada boot de teste de
  integração via WebApplicationFactory); como é idempotente e rápido, não afetou a suíte.
- BackgroundService não coberto por teste (decisão do brief — flakiness); a lógica testável vive no handler.
