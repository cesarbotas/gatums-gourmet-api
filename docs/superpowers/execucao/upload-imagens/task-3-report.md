# Task 3 — relatório de implementação (S3ImageStorage + DI + config)

Implementer: subagent fresco (general-task-execution). Build da solution: 0 erros, 14 avisos pré-existentes.

## Arquivos criados/alterados
- Criados (pasta final `Services/Storage/` após ajuste do orquestrador):
  - `src/GatumsGourmet.Infrastructure/Services/Storage/StorageOptions.cs`
  - `src/GatumsGourmet.Infrastructure/Services/Storage/S3ImageStorage.cs`
  - `src/GatumsGourmet.Infrastructure/Services/Storage/UnconfiguredImageStorage.cs`
- Alterados:
  - `src/GatumsGourmet.Infrastructure/GatumsGourmet.Infrastructure.csproj` (+AWSSDK.S3 4.0.104.7)
  - `src/GatumsGourmet.Infrastructure/InfrastructureServiceCollectionExtensions.cs` (DI condicional)
  - `src/GatumsGourmet.Api/appsettings.json` (Storage = R2/Production defaults)
  - `src/GatumsGourmet.Api/appsettings.Development.json` (Storage = MinIO)
  - `src/GatumsGourmet.Api/appsettings.LocalDocker.json` (Storage = MinIO)
  - `src/GatumsGourmet.Api/appsettings.Release.json` (Storage = Supabase)

## Decisões
- **AWSSDK.S3 4.0.104.7** (V4 GA, estável out/2026, compatível .NET 10). Pinada.
- **`IAmazonS3` registrado no DI** (Singleton, thread-safe/reusável) para testabilidade; `IImageStorage` Scoped (coerente com o resto do DI).
- **DI condicional**: `if (!string.IsNullOrWhiteSpace(storageOptions?.ServiceUrl))` → `S3ImageStorage`, senão `UnconfiguredImageStorage`.
- **PutObjectRequest.DisablePayloadSigning = ForcePathStyle** (compat MinIO/R2 no V4). `AutoCloseStream = false` (stream é do chamador).

## Desvio corrigido pelo orquestrador
- O brief pedia `Services/Imagens/`, mas spec §8 e plano dizem `Services/Storage/` (decisão já validada). O orquestrador moveu os 3 arquivos para `Services/Storage/` e ajustou namespace + using no DI, depois rebuildou (0 erros). Brief estava desatualizado; prevalece a spec/plano.

## Dívidas deferidas
- Compat real MinIO (checksum/payload signing) só exercitada na Task 5/6 contra o MinIO do docker-compose.
- Sem teste unitário nesta task (por design; cobertura via integração na Task 5).
- R2/Supabase `PublicBaseUrl` são placeholders (`<accountid>`/`<project-ref>`) — ajustar domínio público real no deploy.
