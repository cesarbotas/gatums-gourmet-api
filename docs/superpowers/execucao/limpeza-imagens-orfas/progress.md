# Limpeza de imagens órfãs (background service)

Branch `feature/limpeza-imagens-orfas` (a partir de `develop`). Bounded — espelha o `RefreshTokenCleanupService` que já existia.

## Design aprovado pelo usuário
- Background service estilo RefreshToken (PeriodicTimer, intervalo configurável, resolve handler via scope, loga, engole exceção).
- "Órfã" = objeto no prefixo `produtos/` cuja URL NÃO está em nenhum `Produto.FotoUrl`.
- Fonte de verdade = `Produto.FotoUrl` (snapshots em Pedido/Carrinho NÃO contam).
- **Grace = início do dia de hoje (00:00 UTC)**: só apaga objetos modificados ANTES de hoje; tudo de hoje fica protegido (ex.: upload feito mas produto não salvo).

## Implementação
- Domain: `IImageStorage` ganhou `ListObjectsAsync(prefixo)` → `IReadOnlyList<StoredObject>` e `DeleteAsync(objectKey)`. Novo record `StoredObject(ObjectKey, Url, LastModifiedUtc)`.
- Domain: `IProdutoRepository.ListFotoUrlsEmUsoAsync()` → `IReadOnlyList<string>` (SELECT DISTINCT FotoUrl não-vazio).
- Application: `PurgeOrphanImagensHandler` + `IPurgeOrphanImagensHandler` (`Result<int>` = qtd removida). Corte = `clock.UtcNow.Date`. Pula se em uso OU dentro da carência.
- Infrastructure: `S3ImageStorage.ListObjectsAsync` (ListObjectsV2 paginado, monta URL igual ao upload) + `DeleteAsync` (DeleteObject). `UnconfiguredImageStorage.ListObjectsAsync` devolve vazio (no-op silencioso); `DeleteAsync`/`UploadAsync` lançam erro claro. `ProdutoRepository.ListFotoUrlsEmUsoAsync`.
- Api: `OrphanImageCleanupService : BackgroundService` (intervalo `Storage:OrphanCleanupIntervalHours`, default 24h), registrado em Program.cs ao lado do RefreshToken.
- DI: handler Scoped em ApplicationServiceCollectionExtensions.

## Decisões
- Grace por "início do dia" (clock.UtcNow.Date) em vez de "N horas atrás" — escolha do usuário; mais simples e seguro (nada enviado hoje some).
- Unconfigured storage → ListObjects vazio (não falha o job de manutenção quando não há storage, ex.: ambiente sem config).
- Match por URL pública exata (OrdinalIgnoreCase) entre objeto e FotoUrl. URLs externas (placehold.co do seed) nunca casam o prefixo do bucket → não entram na varredura (só listamos `produtos/`).

## TDD
- RED: `tests/UnitTests/.../PurgeOrphanImagens/PurgeOrphanImagensHandlerTests.cs` — órfã+antiga deleta; em uso não; órfã de hoje não (grace); misto deleta só a órfã antiga. 4 casos.
- GREEN: handler + contratos. 4/4.
- Integração real: `tests/IntegrationTests/.../PurgeOrphanImagens/S3ImageStorageListDeleteTests.cs` — contra MinIO: upload 2, lista vê os 2, deleta 1, lista confirma 1. Skip suave se 9000 fechada. Passou.

## Verificação
- Build solution: 0 erros. Unit: 164/164 (160 + 4). Integração: 117/117 (116 + 1 real MinIO).

## Status
Concluído. Pronto para PR no develop.
Dívida quitada: era a última dívida registrada (limpeza de órfãs).
