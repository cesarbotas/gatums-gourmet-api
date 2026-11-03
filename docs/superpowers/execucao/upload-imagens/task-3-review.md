# Task 3 — review (S3ImageStorage + DI condicional + config por ambiente)

Verdict: **APPROVED** (zero Critical/Important). 3 findings Minor/informativos.

## Resumo
Task 3 conecta um backend S3-compatível (`AWSSDK.S3` 4.0.104.7) atrás da abstração
`IImageStorage`. `S3ImageStorage` só é registrado quando `Storage:ServiceUrl` está
preenchido; caso contrário o fallback `UnconfiguredImageStorage` é vinculado para
o host subir normalmente e qualquer upload falhar com mensagem explícita (não NRE,
não 500 opaco de causa desconhecida). Seção `Storage` semeada nos 4 appsettings
(MinIO em Development/LocalDocker, Supabase em Release, R2 no base), com segredos
reais vazios para injeção por ambiente. Object key `{prefixo}/{guid:N}.{extensao}`,
URL pública com guarda de barra final.

## Review focus (risco declarado): storage não configurado
VERIFICADO. O branch `!string.IsNullOrWhiteSpace(storageOptions?.ServiceUrl)` cobre
tanto "sem bloco Storage" (`Get<StorageOptions>()` → null, guardado por `?.`) quanto
"ServiceUrl vazio". Host sobe normalmente (cliente S3 só é construído lazy dentro da
factory singleton). No upload real, lança `InvalidOperationException` com mensagem
clara — não é NRE. Nuance: essa exceção mapeia para HTTP 500 "Internal Server Error"
via `ExceptionMiddleware`, com a mensagem clara no `ProblemDetails.Detail` e no log.
Spec §3 sancionou "400/500 com mensagem clara" → em conformidade, não bloqueante.

## Outras checagens (todas passando)
- Object key adiciona o ponto corretamente (detector devolve extensão sem ponto). Sem ponto duplo.
- URL pública usa `PublicBaseUrl.TrimEnd('/')` → sem barra dupla.
- `AutoCloseStream = false` deixa o MemoryStream (disposto via `using` no handler) com o chamador — sem double-dispose.
- `DisablePayloadSigning = ForcePathStyle` adequado p/ MinIO/R2 no SDK V4.
- Sem segredos commitados: minioadmin só em Dev/LocalDocker; Release (Supabase) e base (R2) com AccessKey/SecretKey vazios + nota sobre `Storage__*`.
- Convenções: uma classe por arquivo, namespace file-scoped, bloco DI espelha o do ViaCEP.

## Findings (todos Minor/informativos)
1. Upload não configurado retorna 500 (não 4xx) — em conformidade com a spec §3; se quiser sinal mais nítido, rotear por tipo da família DomainException.
2. Pasta `Services/Storage` (spec §8) vs `Services/Imagens` do brief — **resolvido antes do review**: o orquestrador moveu para `Services/Storage` para casar com a spec/plano (decisão já validada). O brief estava desatualizado.
3. Sem cobertura automatizada do config gate ainda — chega na Task 5 (testes de integração).

Documento original do reviewer: semantic-review/ (scratch, git-ignored).
