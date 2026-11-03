# Fix: diagnóstico de storage no startup + 400 claro quando não configurado

Branch `fix/storage-diagnostico-e-400` (a partir de `develop`). Origem: no Render (ambiente Release),
o upload de imagem retornava **500** com "Storage de imagens não configurado" (ServiceUrl vazio),
mesmo com a env var `Storage__ServiceUrl` setada e após redeploy. O cleanup de órfãs "funcionava"
porque o `UnconfiguredImageStorage.ListObjectsAsync` retorna vazio sem lançar (no-op silencioso).

## Diagnóstico (brainstorm com o usuário)
- Causa do 500: quando `Storage:ServiceUrl` está vazio, o DI injeta `UnconfiguredImageStorage`,
  cujo `UploadAsync` lança `InvalidOperationException`. O `ExceptionMiddleware` só mapeia a família
  `DomainException` para 4xx; qualquer outra exceção vira **500 opaco**.
- Causa raiz do "ServiceUrl vazio": o `appsettings.Release.json` tem `ServiceUrl: ""`, e a env var
  `Storage__ServiceUrl` do Render **não está chegando ao processo** (nome confirmado correto pelo
  usuário; redeploy feito). Suspeitas vivas: escopo/vínculo da var no Render (Environment Group não
  linkado, Secret File em vez de env var, valor em branco). O CÓDIGO está correto (ordem de config
  padrão; nada re-adiciona fontes — confirmado por grep).

## Decisões do usuário
- Ambiente: Release (manter). Env vars com `__`.
- Problema 2: opção **B** — handler detecta storage não configurado e devolve `Result.Fail` (→ 400).
- Quer **log de diagnóstico** para validar no Render, e "tudo logado".

## Implementação (TDD)
- **Opção B (handler):** `UploadImagemHandler` agora recebe `ILogger<>` e checa `config["Storage:ServiceUrl"]`
  no início; se vazio → `Result.Fail("Upload de imagem indisponível: storage não configurado.")` ANTES de
  tocar o storage. Lê via `IConfiguration` (NÃO via `IOptions<StorageOptions>`, que é tipo da Infra — a
  Application não referencia Infra; Clean Architecture preservada). Logs em cada etapa (início, guardas,
  validação, envio, conclusão).
- **Item 1 (diagnóstico):** `Api/Extensions/StorageDiagnosticsExtensions.cs` → `app.LogStorageDiagnostics()`
  chamado no `Program.cs` no startup. Loga ambiente, configurado (bool), ServiceUrl/PublicBaseUrl/AccessKey/
  SecretKey como **bool preenchido** (sem vazar segredo), Bucket, Region, e qual implementação foi registrada.
  WARNING explícito se ServiceUrl vazia. Feito na camada Api (tem logging real) em vez de na Infra (evita
  dep de console-logging na Infra e loga pelo pipeline do host → aparece no Render).
- Refactor trivial no DI: `var storageConfigurado = ...` (mesmo comportamento).

## RED→GREEN
- Novo teste `UploadImagemHandlerTests.Handle_StorageNaoConfigurado_Falha_SemAcionarStorage`:
  config com `ServiceUrl=""` → espera Fail + storage nunca acionado (Times.Never). RED antes, GREEN depois.
- Os testes existentes do handler ganharam `ServiceUrl` preenchida na config feliz + `NullLogger`.

## Verificação
- Build solution: 0 erros. Unit: 165/165. Integração: 121/121 (upload real contra MinIO incluso).

## Dívida/observação
- O `appsettings.Release.json` com `ServiceUrl: ""` mascara a env var ausente (vira vazio silencioso).
  Discutido com o usuário como item 3 (remover o `""`), NÃO aplicado nesta branch (fora do escopo pedido:
  só 1 e 2). Fica como sugestão.
- A opção B melhora a MENSAGEM (400 claro), mas NÃO faz o upload funcionar — isso depende da env var
  chegar no Render. O log de diagnóstico (item 1) é o que vai confirmar, no próximo redeploy, se a
  ServiceUrl chegou ao processo.

## Status
Concluído (código). Pronto para PR. Próximo passo do usuário: deployar no Render e ler o log
"[Storage] Diagnóstico no startup: ..." para confirmar se ServiceUrlPreenchida=true/false.
