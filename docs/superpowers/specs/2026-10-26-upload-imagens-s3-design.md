# SDD — Upload de imagens em storage S3-compatível por ambiente

- **Data:** 2026-10-26
- **Branch:** `feature/gravar-imagem-s3`
- **Status:** Em revisão
- **Escopo:** Subsistema de upload/armazenamento de imagens de produto, com storage S3-compatível configurável por ambiente (local: MinIO; homologação: Supabase Storage; produção: Cloudflare R2). O endpoint de upload retorna a URL pública; o cadastro de produto continua gravando apenas a string `FotoUrl`.

---

## 1. Objetivo e contexto

Hoje o cadastro de produto tem `FotoUrl` (string), mas não há onde hospedar a imagem. Esta feature adiciona um endpoint de upload que recebe o arquivo, valida, grava num bucket S3-compatível e devolve a URL pública — que o front usa no cadastro/edição do produto.

### Premissas (decididas no brainstorming)
- **Upload via API (proxy)**, não presigned URL: o front envia o arquivo à API, que valida e repassa ao storage.
- **Endpoint separado** (`POST /api/v1/imagens`), não embutido no endpoint de produto. O produto permanece 100% JSON com `FotoUrl` string.
- **Um único cliente S3** (`AWSSDK.S3`) para os três provedores — todos são S3-compatíveis; só a configuração muda por ambiente. A flexibilidade de trocar backend mora na interface de domínio `IImageStorage`, não no cliente.
- **Permissão:** `produtos:escrever` (reaproveita o RBAC existente).
- **Validação:** tipos JPEG/PNG/WebP, máximo 5 MB, nome gerado pela API (`produtos/{guid}.{ext}`), tipo verificado por **magic bytes** (não confia no content-type do cliente).
- **Leitura pública:** buckets em modo leitura-pública; a URL é montada a partir de uma base configurável por ambiente.

### Avaliação de viabilidade dos provedores (tier grátis)
- **MinIO (local):** S3-compatível, roda em Docker. Console web embutido na porta 9001. Zero custo. Viável.
- **Supabase Storage (hml):** tier grátis ~1 GB; expõe endpoint S3 (`/storage/v1/s3`) e URL pública (`/storage/v1/object/public/...`). Viável; atenção à pausa por inatividade do projeto free.
- **Cloudflare R2 (prd):** tier grátis 10 GB + egress gratuito, S3-compatível. É o **R2** (storage), não o "Cloudflare Images". A URL pública exige habilitar bucket público (domínio r2.dev) ou custom domain. Viável e a melhor das três para PRD.

### Fora de escopo
- Presigned URLs / upload direto do front ao storage.
- Transformações/otimização de imagem (resize, thumbnails).
- Exclusão de imagem órfã (quando um produto troca de foto, a antiga fica no bucket) — registrar como dívida futura.
- Refatorar os endpoints Clientes/Categorias/Pedidos para o padrão `[AsParameters]` + subpasta — **tarefa separada** (só Imagens já nasce no padrão).

---

## 2. Arquitetura e abstração

Mesmo padrão de `ICepProvider`/`IPaymentGateway`: interface no Domain, implementação na Infrastructure, config por ambiente.

### Domain — `Services/Interfaces/IImageStorage.cs`
```
public record ImageUploadResult(string Url, string ObjectKey);

public interface IImageStorage
{
    Task<ImageUploadResult> UploadAsync(Stream conteudo, string contentType, string extensao, string prefixo, CancellationToken ct = default);
}
```
- `prefixo` = pasta lógica no bucket (ex.: `"produtos"`). O object key final é `{prefixo}/{guid}.{extensao}`.
- Retorna a URL pública completa (montada a partir de `PublicBaseUrl`) e o object key.

### Infrastructure — `Services/Storage/S3ImageStorage.cs`
- Depende de `IAmazonS3` (AWSSDK.S3) e das opções `StorageOptions`.
- `UploadAsync`: gera object key, faz `PutObjectAsync` com o stream e o content-type, monta a URL `{PublicBaseUrl}/{objectKey}`.
- Uma classe por arquivo. Pacote novo: `AWSSDK.S3` no `.csproj` da Infrastructure.
- `StorageOptions` (bind da seção `Storage`): `ServiceUrl`, `Bucket`, `PublicBaseUrl`, `AccessKey`, `SecretKey`, `ForcePathStyle`, `MaxFileSizeMB`.

### DI (Infrastructure)
Registro condicional: se `Storage:ServiceUrl` estiver configurado, registra `IAmazonS3` (com `AmazonS3Config { ServiceURL, ForcePathStyle, credenciais }`) e `IImageStorage → S3ImageStorage`. Sem config, o endpoint de upload responde erro de configuração ao ser chamado (não derruba o host).

---

## 3. Endpoint e use case

### Endpoint — `Api/Endpoints/V1/Imagens/`
Segue o padrão de subpasta do `Produtos/`.
- `ImagensRequest.cs` — `public IFormFile Arquivo { get; set; }` com `[FromForm]`.
- `ImagensEndpoints.cs` — `POST /` aceita `multipart/form-data`, `RequirePermission("produtos:escrever")`, `.DisableAntiforgery()`, lê o `IFormFile` e repassa ao handler. Responde 201 com `{ url }`.
  - O endpoint chama `arquivo.OpenReadStream()` e passa ao handler: o stream, `arquivo.ContentType`, `arquivo.FileName` e `arquivo.Length`.
  - `.DisableAntiforgery()` é necessário para upload de form em Minimal API e é seguro aqui: a API é stateless com Bearer (sem cookie/sessão), então não há vetor CSRF que o antiforgery protegeria.
- Mapear em `EndpointRouteBuilderExtensions`: `ImagensEndpoints.Map(v1.MapGroup("/imagens").WithTags("Imagens"));`

### Use case — `Application/UseCases/V1/Imagens/UploadImagem/`
- `IUploadImagemHandler.Handle(Stream conteudo, string contentType, string? nomeArquivo, long tamanho, CancellationToken ct) → Result<UploadImagemResponse>`.
- `UploadImagemResponse(string Url)`.
- Fluxo:
  1. `tamanho <= 0` → `Fail("Arquivo vazio.")`.
  2. `tamanho > MaxFileSizeMB*1MB` → `Fail("Arquivo excede o tamanho máximo de {N} MB.")`.
  3. Detecta o tipo real por **magic bytes** (lê os primeiros bytes do stream). Se não for JPEG/PNG/WebP → `Fail("Tipo de imagem não suportado. Use JPEG, PNG ou WebP.")`.
  4. Deriva a extensão do tipo detectado (`jpg`/`png`/`webp`) e o content-type canônico.
  5. `stream.Position = 0`; chama `IImageStorage.UploadAsync(stream, contentType, extensao, "produtos", ct)`.
  6. Retorna `Ok(new UploadImagemResponse(result.Url))`.
- Depende de `IImageStorage`, `IConfiguration` (para `MaxFileSizeMB`), e um helper de detecção.

### Helper de detecção — `Application/UseCases/V1/Imagens/ImageTypeDetector.cs` (ou Domain)
- `static (bool Ok, string Extensao, string ContentType) Detectar(ReadOnlySpan<byte> prefixo)`.
- Assinaturas: JPEG `FF D8 FF`; PNG `89 50 4E 47 0D 0A 1A 0A`; WebP `RIFF....WEBP` (bytes 0-3 = `RIFF`, 8-11 = `WEBP`).
- Testável isoladamente.

### Erros → HTTP
Todos os `Fail` viram 400 (padrão do `ResultExtensions`). Sem permissão → 403. Sem config de storage → 400/500 com mensagem clara.

---

## 4. Configuração por ambiente

Seção `Storage` no appsettings (mesmos campos; só valores mudam). **Segredos (AccessKey/SecretKey) via variáveis de ambiente/secrets**, nunca commitados — exceto as credenciais locais do MinIO (sem valor de segurança).

```json
"Storage": {
  "ServiceUrl": "",
  "Bucket": "gatums-imagens",
  "PublicBaseUrl": "",
  "AccessKey": "",
  "SecretKey": "",
  "ForcePathStyle": true,
  "MaxFileSizeMB": 5
}
```

| Ambiente | Arquivo | ServiceUrl | PublicBaseUrl | Chaves |
|---|---|---|---|---|
| Local | `appsettings.Development.json` | `http://localhost:9000` | `http://localhost:9000/gatums-imagens` | `minioadmin`/`minioadmin` (local, no arquivo) |
| Local Docker | `appsettings.LocalDocker.json` | `http://localhost:9000` | `http://localhost:9000/gatums-imagens` | `minioadmin`/`minioadmin` |
| Homologação (ambiente `Release`, branch `release`) | `appsettings.Release.json` | `https://{projeto}.supabase.co/storage/v1/s3` | `https://{projeto}.supabase.co/storage/v1/object/public/gatums-imagens` | secret (Render) |
| Produção | `appsettings.Production.json` | `https://{account_id}.r2.cloudflarestorage.com` | domínio público do R2 (r2.dev/custom) | secret |

`ForcePathStyle = true` para MinIO e R2; Supabase funciona com path style no endpoint S3.

### Docker (local)
Adicionar ao `docker/docker-compose.yml`:
- Serviço `minio` (imagem `minio/minio`), portas `9000` (API S3) e `9001` (**console web** — validação visual dos uploads locais), credenciais `minioadmin`/`minioadmin`, volume para persistência.
- Serviço `minio-setup` (imagem `minio/mc`, roda uma vez): cria o bucket `gatums-imagens` e aplica policy de leitura pública (download anônimo).

---

## 5. Testes

- **Unit** (`tests/.../V1/Imagens/UploadImagem/UploadImagemHandlerTests.cs`) mockando `IImageStorage`:
  - Imagem JPEG válida → `Ok` com URL retornada pelo storage; `UploadAsync` chamado 1×.
  - `tamanho <= 0` → Fail "Arquivo vazio."; storage não chamado.
  - tamanho acima do limite → Fail; storage não chamado.
  - conteúdo com magic bytes inválidos (ex.: texto puro) → Fail "Tipo não suportado."; storage não chamado.
- **Unit** do `ImageTypeDetector`: JPEG/PNG/WebP válidos detectados; lixo → não detectado.
- **Integração** (`Buscar* style`): substitui `IImageStorage` por fake na `CustomWebApplicationFactory`; `POST /imagens` multipart com imagem válida → 201 + URL; sem token/permissão → 401/403; tipo inválido → 400. Offline (não bate no MinIO real).

---

## 6. Documentação (entregável)

- **README.md**: nova seção "Armazenamento de imagens" na evolução do projeto — matriz de ambientes (MinIO/Supabase/R2), fluxo front (upload → URL → produto), como subir o MinIO local (+ console 9001).
- **docs/Site/Upload-Imagens.md** (estilo `Busca-CEP.md`): setup passo a passo de cada provedor (criar bucket, leitura pública, chaves) e **referência para o front** (multipart, consumir a URL, usar no cadastro de produto; exemplo React + o `apiFetch` existente).
- **docs/Site/API-Reference.md**: entrada do `POST /imagens`.
- **Postman**: request de upload (form-data) na coleção.

---

## 7. Fluxo do front (referência)

```
1. POST /api/v1/imagens  (multipart: arquivo=<file>, Bearer com produtos:escrever)
   → 201 { "url": "https://.../produtos/{guid}.jpg" }
2. POST ou PUT /api/v1/produtos com fotoUrl = url recebida
```
O produto grava só a string. Trocar a foto = novo upload + atualizar `fotoUrl`.

---

## 8. Estrutura de pastas (alvo)

```
Domain/
  Services/Interfaces/IImageStorage.cs      (interface + record ImageUploadResult)
Infrastructure/
  Services/Storage/S3ImageStorage.cs
  Services/Storage/StorageOptions.cs
  InfrastructureServiceCollectionExtensions.cs  (registro IAmazonS3 + IImageStorage)
Application/
  UseCases/V1/Imagens/ImageTypeDetector.cs
  UseCases/V1/Imagens/UploadImagem/{IUploadImagemHandler,UploadImagemHandler,UploadImagemResponse}.cs
  ApplicationServiceCollectionExtensions.cs      (registro do handler)
Api/
  Endpoints/V1/Imagens/{ImagensEndpoints,ImagensRequest}.cs
  Extensions/EndpointRouteBuilderExtensions.cs   (mapear /imagens)
  appsettings.{json,Development,LocalDocker,Release,Production}.json  (seção Storage)
docker/docker-compose.yml                        (minio + minio-setup)
docs/Site/Upload-Imagens.md · README.md · API-Reference.md · Postman
```
