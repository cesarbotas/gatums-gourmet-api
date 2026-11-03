# Review FINAL da feature upload-imagens (branch inteira)

Verdict: **APPROVED** (zero Critical/Important). Review da feature como um todo (range 2b79f0a..HEAD), além dos reviews por task.

## Áreas verificadas (todas OK)
- **Caminho end-to-end**: endpoint→handler→storage com contrato casado; nada chega ao storage sem passar pela checagem de magic bytes.
- **Segurança**: `produtos:escrever` aplicado; tipo por inspeção de conteúdo (não header/filename); object key gerada por GUID (sem path traversal); `ServiceUrl` é só config (sem SSRF); só credenciais locais do MinIO commitadas, chaves reais vazias/injetadas por env.
- **Fix MinIO/HTTP**: `RequestChecksumCalculation = WHEN_REQUIRED` no único ponto de construção do `AmazonS3Config`, cobre os 3 ambientes; sem downside em S3/R2 reais; `DisablePayloadSigning` corretamente removido.
- **Storage não configurado**: `InvalidOperationException` → 500 ProblemDetails com Detail claro, host sobe normalmente (spec §3 + ruling do ledger).
- **Streams**: 3 donos não sobrepostos, `AutoCloseStream=false`, MemoryStream rebobinado — sem leak/double-dispose.
- **Consistência**: ProblemDetails e 201 via ResultExtensions.
- **Testes com teeth**: casos offline asseguram storage chamado/não-chamado; (B) MinIO registra o S3ImageStorage real e só pula com a porta 9000 fechada — não mascara falha real de PUT.

## Findings (3 Minor) e tratamento
1. **Teto de tamanho confiava no IFormFile.Length** (Kestrel ~30 MB como teto efetivo). → **RESOLVIDO pós-review**: endpoint agora tem `RequestSizeLimitAttribute` + `RequestFormLimitsAttribute(MultipartBodyLengthLimit)` = ~6 MB, logo acima do limite de negócio de 5 MB. Build 0 erros; UploadImagemEndpointTests 5/5 mantidos. Doc atualizada.
2. **Sem appsettings.Production.json** (spec §8 lista). Deliberado por ruling do ledger (prd herda R2 do appsettings.json base + env vars). Mantido; divergência spec/arquivos é consciente.
3. **WebP valida só o contêiner RIFF/WEBP** (não VP8/VP8L/VP8X). Suficiente p/ o escopo; já diferido no ledger.

## Nota de escopo
`git diff main..HEAD` mostra ~419 arquivos porque a main antecede features já mergeadas (auth RBAC, testes de pedidos/pagamentos). A feature são os 17 commits a partir de 2b79f0a (escopo do review package). Nenhuma mudança não-relacionada foi embutida nos commits da feature.
