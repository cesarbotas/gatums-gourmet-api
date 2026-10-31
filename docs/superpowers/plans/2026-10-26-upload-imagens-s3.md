# Upload de Imagens (storage S3-compatível) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adicionar um endpoint de upload de imagem (`POST /api/v1/imagens`) que valida o arquivo, grava num storage S3-compatível (MinIO local / Supabase hml / Cloudflare R2 prd) e devolve a URL pública; o produto continua gravando só `FotoUrl` (string).

**Architecture:** Interface `IImageStorage` no Domain; implementação única `S3ImageStorage` (AWSSDK.S3) na Infrastructure, configurada por ambiente via seção `Storage`. Use case `UploadImagem` na Application valida tamanho + tipo (magic bytes) e chama o storage. Endpoint Minimal API multipart. MinIO + console no docker local.

**Tech Stack:** .NET 10, AWSSDK.S3, EF Core (inalterado), xUnit + Moq + FluentAssertions, Docker (MinIO).

**Spec:** `docs/superpowers/specs/2026-10-26-upload-imagens-s3-design.md`

## Global Constraints

- net10.0, noBOM, uma classe por arquivo. Seguir o padrão do repo (use case = Handler + Interface + Request/Response; `Result<T>`; endpoint estático `Map(RouteGroupBuilder)`).
- DI manual em `ApplicationServiceCollectionExtensions` (handler) e `InfrastructureServiceCollectionExtensions` (storage + `IAmazonS3`).
- Permissão do upload: `produtos:escrever` (via `.RequirePermission(...)` de `GatumsGourmet.Api.Authorization`).
- Validação: JPEG/PNG/WebP por **magic bytes** (não confiar no content-type do cliente); tamanho máximo `Storage:MaxFileSizeMB` (default 5); object key gerado = `produtos/{guid}.{ext}`.
- Falha de negócio → 400 (via `ResultExtensions.ToHttpResult`). Sem permissão → 403.
- URL pública = `{Storage:PublicBaseUrl}/{objectKey}`. Segredos (AccessKey/SecretKey) por ambiente, nunca no git (exceto credenciais locais do MinIO).
- Tudo em `/api/v1`.

## Review Focus

- **Content-type mentido** (arquivo .exe renomeado para .jpg) → magic bytes rejeitam com 400, não sobe ao storage. (Task 2)
- **Arquivo acima do limite** (> MaxFileSizeMB) → 400 antes de tocar o storage. (Task 2)
- **Upload sem a permissão `produtos:escrever`** (anônimo/cliente) → 401/403, nunca 200. (Task 4)
- **Storage não configurado** (`Storage:ServiceUrl` vazio) → erro claro, não `NullReferenceException`/500 opaco. (Task 3)
- **WebP** (assinatura RIFF/WEBP, menos óbvia que JPEG/PNG) → detectado como válido. (Task 1)

---

## File Structure

- **Domain:** `Services/Interfaces/IImageStorage.cs` (interface + record `ImageUploadResult`).
- **Application:** `UseCases/V1/Imagens/ImageTypeDetector.cs`; `UseCases/V1/Imagens/UploadImagem/{IUploadImagemHandler,UploadImagemHandler,UploadImagemResponse}.cs`.
- **Infrastructure:** `Services/Storage/StorageOptions.cs`, `Services/Storage/S3ImageStorage.cs`; `.csproj` (+AWSSDK.S3); `InfrastructureServiceCollectionExtensions.cs` (registro).
- **Api:** `Endpoints/V1/Imagens/{ImagensEndpoints,ImagensRequest}.cs`; `Extensions/EndpointRouteBuilderExtensions.cs` (mapear); `appsettings.{json,Development,LocalDocker,Release,Production}.json` (seção `Storage`).
- **Application DI:** `ApplicationServiceCollectionExtensions.cs` (handler).
- **Docker:** `docker/docker-compose.yml` (minio + minio-setup).
- **Tests:** `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Imagens/...`; `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Imagens/...`.
- **Docs:** `README.md`, `docs/Site/Upload-Imagens.md`, `docs/Site/API-Reference.md`, `docs/Postman/*`.

---

## Task 1: Detecção de tipo por magic bytes + contrato de domínio

**Files:**
- Create: `src/GatumsGourmet.Domain/Services/Interfaces/IImageStorage.cs`
- Create: `src/GatumsGourmet.Application/UseCases/V1/Imagens/ImageTypeDetector.cs`
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Imagens/ImageTypeDetectorTests.cs`

**Interfaces:**
- Produces:
  - `record ImageUploadResult(string Url, string ObjectKey);` e `interface IImageStorage { Task<ImageUploadResult> UploadAsync(Stream conteudo, string contentType, string extensao, string prefixo, CancellationToken ct = default); }` em `GatumsGourmet.Domain.Services.Interfaces`.
  - `static class ImageTypeDetector { static (bool Ok, string Extensao, string ContentType) Detectar(ReadOnlySpan<byte> prefixo); }` em `GatumsGourmet.Application.UseCases.V1.Imagens`.

- [ ] **Step 1: Escrever o teste** `ImageTypeDetectorTests` com `[Theory]`:
  - JPEG (`FF D8 FF E0`) → `(true, "jpg", "image/jpeg")`.
  - PNG (`89 50 4E 47 0D 0A 1A 0A`) → `(true, "png", "image/png")`.
  - WebP (`52 49 46 46 00 00 00 00 57 45 42 50`) → `(true, "webp", "image/webp")` (REVIEW FOCUS).
  - texto puro (`48 65 6C 6C 6F`) → `(false, _, _)`.
  - buffer vazio → `(false, _, _)`.
- [ ] **Step 2: Rodar e ver falhar.** Run: `dotnet test tests/GatumsGourmet.UnitTests --filter ImageTypeDetectorTests`
- [ ] **Step 3: Criar** `IImageStorage` (+record) e implementar `ImageTypeDetector.Detectar` (compara os prefixos; WebP exige `RIFF` nos bytes 0-3 e `WEBP` nos bytes 8-11).
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: contrato IImageStorage e deteccao de tipo por magic bytes`

---

## Task 2: Use case UploadImagem

**Files:**
- Create: `src/GatumsGourmet.Application/UseCases/V1/Imagens/UploadImagem/IUploadImagemHandler.cs`, `UploadImagemHandler.cs`, `UploadImagemResponse.cs`
- Modify: `src/GatumsGourmet.Application/ApplicationServiceCollectionExtensions.cs` (registro)
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Imagens/UploadImagem/UploadImagemHandlerTests.cs`

**Interfaces:**
- Consumes: `IImageStorage`, `ImageTypeDetector` (Task 1), `IConfiguration`.
- Produces: `record UploadImagemResponse(string Url);` e `interface IUploadImagemHandler { Task<Result<UploadImagemResponse>> Handle(Stream conteudo, string? contentType, string? nomeArquivo, long tamanho, CancellationToken ct = default); }`.

- [ ] **Step 1: Escrever os testes** `UploadImagemHandlerTests` (mock `IImageStorage` + `IConfiguration` InMemory com `Storage:MaxFileSizeMB=5`):
  - JPEG válido (stream com magic bytes de JPEG, tamanho ok) → `Ok`; `Url` == a retornada pelo mock do storage; `UploadAsync(..., "produtos", ...)` chamado 1×.
  - `tamanho = 0` → `Fail("Arquivo vazio.")`; storage não chamado.
  - tamanho `6*1024*1024` → `Fail` contendo "tamanho máximo"; storage não chamado (REVIEW FOCUS).
  - conteúdo de texto puro (magic bytes inválidos) → `Fail("Tipo de imagem não suportado. Use JPEG, PNG ou WebP.")`; storage não chamado (REVIEW FOCUS — content-type mentido).
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** o handler conforme spec §3: valida tamanho → lê os primeiros bytes (ex.: 12) do stream para `ImageTypeDetector.Detectar` → se inválido Fail → `stream.Position = 0` → `storage.UploadAsync(stream, contentType canônico, extensao, "produtos", ct)` → `Ok(new UploadImagemResponse(result.Url))`. `MaxFileSizeMB` de `IConfiguration` (default 5). Registrar `IUploadImagemHandler→UploadImagemHandler` no DI.
- [ ] **Step 4: Rodar e ver passar.** Run: `dotnet test tests/GatumsGourmet.UnitTests --filter UploadImagemHandlerTests`
- [ ] **Step 5: Commit** `feat: use case UploadImagem com validacao de tamanho e tipo`

---

## Task 3: Implementação S3 + configuração + DI

**Files:**
- Modify: `src/GatumsGourmet.Infrastructure/GatumsGourmet.Infrastructure.csproj` (+`AWSSDK.S3`)
- Create: `src/GatumsGourmet.Infrastructure/Services/Storage/StorageOptions.cs`, `S3ImageStorage.cs`, `UnconfiguredImageStorage.cs`
- Modify: `src/GatumsGourmet.Infrastructure/InfrastructureServiceCollectionExtensions.cs`
- Modify: `src/GatumsGourmet.Api/appsettings.json`, `appsettings.Development.json`, `appsettings.LocalDocker.json`, `appsettings.Release.json`, `appsettings.Production.json`
- Test: (sem teste unitário — cobertura via integração na Task 5; build valida)

**Interfaces:**
- Consumes: `IImageStorage` (Task 1), `IConfiguration`.
- Produces: `StorageOptions { ServiceUrl, Bucket, PublicBaseUrl, AccessKey, SecretKey, bool ForcePathStyle, int MaxFileSizeMB }`; `S3ImageStorage : IImageStorage` (depende de `IAmazonS3` + `StorageOptions`). Gera object key `{prefixo}/{Guid.NewGuid():N}.{extensao}`, `PutObjectAsync` no bucket, retorna `ImageUploadResult($"{PublicBaseUrl}/{objectKey}", objectKey)`.

- [ ] **Step 1: Adicionar `AWSSDK.S3`** (versão estável compatível com net10) ao `.csproj` da Infrastructure.
- [ ] **Step 2: Criar `StorageOptions`** (bind de `Storage`) e `S3ImageStorage`.
- [ ] **Step 3: Registrar no DI** (`AddInfrastructure`): bind de `StorageOptions`; se `Storage:ServiceUrl` não vazio, registrar `IAmazonS3` com `AmazonS3Config { ServiceURL = ServiceUrl, ForcePathStyle = ForcePathStyle }` + `BasicAWSCredentials(AccessKey, SecretKey)`, e `IImageStorage→S3ImageStorage`. Se vazio, registrar `IImageStorage→UnconfiguredImageStorage` (criar em `Services/Storage/UnconfiguredImageStorage.cs`), cujo `UploadAsync` lança `InvalidOperationException("Storage de imagens não configurado (Storage:ServiceUrl).")` (REVIEW FOCUS — erro claro, não NRE/500 opaco).
- [ ] **Step 4: Adicionar a seção `Storage`** nos 5 appsettings conforme a matriz da spec §4 (Development e LocalDocker → MinIO `http://localhost:9000`, bucket `gatums-imagens`, `minioadmin`/`minioadmin`, ForcePathStyle true; Release → Supabase; Production → R2; segredos como `""` placeholder onde vêm de env/secret).
- [ ] **Step 5: Build.** Run: `dotnet build GatumsGourmet.sln`. Expected: 0 erros.
- [ ] **Step 6: Commit** `feat: S3ImageStorage (AWSSDK.S3) + config Storage por ambiente`

---

## Task 4: Endpoint de upload (multipart)

**Files:**
- Create: `src/GatumsGourmet.Api/Endpoints/V1/Imagens/ImagensRequest.cs`, `ImagensEndpoints.cs`
- Modify: `src/GatumsGourmet.Api/Extensions/EndpointRouteBuilderExtensions.cs`
- Test: (via integração na Task 5)

**Interfaces:**
- Consumes: `IUploadImagemHandler` (Task 2), `RequirePermission` (`GatumsGourmet.Api.Authorization`).
- Produces: `POST /api/v1/imagens` (multipart). `ImagensRequest { [FromForm] IFormFile Arquivo }`. Mapear `ImagensEndpoints.Map(v1.MapGroup("/imagens").WithTags("Imagens"))`.

- [ ] **Step 1: Criar `ImagensRequest`** (`[FromForm] public IFormFile Arquivo { get; set; }`) e `ImagensEndpoints`: `group.MapPost("/", ... [FromForm] ImagensRequest req ...)` que faz `req.Arquivo.OpenReadStream()` e chama `handler.Handle(stream, req.Arquivo.ContentType, req.Arquivo.FileName, req.Arquivo.Length, ct)`, `.ToHttpResult(201)`, `.RequirePermission("produtos:escrever")`, `.DisableAntiforgery()`.
- [ ] **Step 2: Mapear** o grupo `/imagens` em `EndpointRouteBuilderExtensions.MapV1Endpoints`.
- [ ] **Step 3: Build.** Run: `dotnet build GatumsGourmet.sln`. Expected: 0 erros.
- [ ] **Step 4: Commit** `feat: endpoint POST /imagens (upload multipart)`

---

## Task 5: Teste de integração do endpoint (storage fake, offline)

**Files:**
- Test: `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Imagens/UploadImagemEndpointTests.cs`

**Interfaces:**
- Consumes: `CustomWebApplicationFactory`, `AuthenticatedClientFactory` (CreateAdminClientAsync/CreateClienteClientAsync), `IImageStorage` (substituído por fake via `ConfigureTestServices` + `RemoveAll`, padrão do `BuscarEnderecoPorCepEndpointTests`).

- [ ] **Step 1: Escrever o teste** `[Collection("Integration")]` com um `FakeImageStorage : IImageStorage` que retorna `new ImageUploadResult("https://fake/produtos/x.jpg", "produtos/x.jpg")`. Monta `MultipartFormDataContent` com `ByteArrayContent` de bytes JPEG (magic `FF D8 FF E0...`), campo `arquivo`:
  - admin (produtos:escrever) → **201** e corpo com `url`.
  - cliente (sem a permissão) → **403** (REVIEW FOCUS).
  - sem token (anônimo) → **401**.
  - admin enviando bytes de texto puro → **400** (tipo inválido).
- [ ] **Step 2: Rodar e ver falhar/passar.** Run: `dotnet test tests/GatumsGourmet.IntegrationTests --filter UploadImagemEndpointTests` (container de teste de pé).
- [ ] **Step 3: Rodar a suíte de integração inteira** para confirmar que não houve regressão; reportar contagem.
- [ ] **Step 4: Commit** `test: integracao do endpoint de upload de imagem (storage fake)`

---

## Task 6: MinIO no docker-compose local (+ console)

**Files:**
- Modify: `docker/docker-compose.yml`

- [ ] **Step 1: Adicionar o serviço `minio`** (`minio/minio`, comando `server /data --console-address ":9001"`, portas `9000:9000` e `9001:9001`, env `MINIO_ROOT_USER=minioadmin`/`MINIO_ROOT_PASSWORD=minioadmin`, volume `minio-data:/data`, healthcheck).
- [ ] **Step 2: Adicionar o serviço `minio-setup`** (`minio/mc`, `depends_on` minio healthy, entrypoint que roda `mc alias set`, `mc mb --ignore-existing local/gatums-imagens`, `mc anonymous set download local/gatums-imagens`) — cria o bucket com leitura pública.
- [ ] **Step 3: Validar o compose.** Run: `docker compose -f docker/docker-compose.yml config --quiet`. Expected: exit 0 (YAML/compose válido).
- [ ] **Step 4: Commit** `chore: MinIO + console e bucket publico no docker-compose local`

---

## Task 7: Documentação (README, guia do front, API-Reference, Postman)

**Files:**
- Modify: `README.md` (seção "Armazenamento de imagens" na evolução do projeto)
- Create: `docs/Site/Upload-Imagens.md`
- Modify: `docs/Site/API-Reference.md` (entrada `POST /imagens`)
- Modify: `docs/Postman/GatumsGourmet.postman_collection.json` (request de upload form-data no grupo de catálogo)

- [ ] **Step 1: Escrever** o `docs/Site/Upload-Imagens.md` (estilo `Busca-CEP.md`): setup de cada provedor (MinIO via docker + console 9001; Supabase bucket público + chaves S3; R2 bucket público + chaves), matriz de ambientes, e referência front (multipart com o `apiFetch`, consumir `url`, usar em `fotoUrl` no produto). Atualizar README (evolução) e API-Reference (`POST /imagens`: multipart `arquivo`, 201 `{ url }`, 400 tipos/tamanho, permissão produtos:escrever). Adicionar o request de upload na coleção Postman (grupo Catálogo, form-data; nota de que o Runner/Newman pode pular por exigir arquivo binário local).
- [ ] **Step 2: Conferir** mojibake (grep) e validar o JSON do Postman (`ConvertFrom-Json`).
- [ ] **Step 3: Build final + suíte completa.** Run: `dotnet build GatumsGourmet.sln`; `dotnet test tests/GatumsGourmet.UnitTests`; `dotnet test tests/GatumsGourmet.IntegrationTests`. Expected: 0 erros, tudo verde.
- [ ] **Step 4: Commit** `docs: guia de upload de imagens, API-Reference e Postman`

---

## Notas de execução

- Container de teste de integração (`gatums-gourmet-db-test-integration`, porta 5433) precisa estar de pé para as Tasks 5 e 7.
- Os testes de integração usam **storage fake** (não sobem MinIO) — offline e determinísticos. O MinIO do docker-compose é para uso/validação manual local (console 9001).
- DI manual: registrar o handler (Application) e o storage (Infrastructure).
- Se a API estiver rodando e travar DLLs no build (MSB3021/MSB3027), encerrar o processo `GatumsGourmet.Api` antes de rebuildar.
