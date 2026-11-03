# Task 6 — relatório (MinIO + bucket no docker-compose) + FIX de produção exposto

Feito pelo orquestrador diretamente (infra de 1 arquivo + correção de bug de produção que o MinIO real revelou). Verificado end-to-end.

## Task 6 — docker-compose
`docker/docker-compose.yml`: serviço `minio` (API 9000 + console 9001), bucket `gatums-imagens` criado público no startup. Volume `miniodata`.

### Desvio relevante: imagem do MinIO
A imagem oficial da comunidade (`minio/minio`, `minio/mc`) foi **removida do Docker Hub (set/2026)** e as do `quay.io` passaram a exigir login (pull anônimo → 401). Testei ambos e falharam. Troquei para **`bitnamilegacy/minio`** (publicamente disponível, S3-compatível), pinada por digest `sha256:451fe685...`. A imagem Bitnami cria o bucket declarativamente via `MINIO_DEFAULT_BUCKETS=gatums-imagens:public`, então **eliminei o container `minio-setup`** (dependia do `minio/mc` indisponível). Credenciais minioadmin/minioadmin, coerentes com appsettings Dev/LocalDocker.
Fontes: remoção das imagens MinIO do Docker Hub/quay (issues documenso#3382, tbpro-add-on#1268). Conteúdo reformulado por conformidade de licenciamento.

### Verificação
`docker compose up -d minio` → container Up (healthy); log confirma "Bucket created successfully local/gatums-imagens" + "policy public". `mc ls` após os testes mostra o objeto `produtos/{guid}.jpg` (16B) gravado pelo teste (B).

## FIX de produção (bug exposto pelo MinIO real — originado na Task 3)
Ao dar teeth ao teste (B) (ver abaixo), o PUT real contra MinIO falhou com **500: "When DisablePayloadSigning is true, the request must be sent over HTTPS."** — ou seja, o `S3ImageStorage` da Task 3 **quebraria TODO upload em local/dev (http://)**.
Causa raiz: o AWS SDK .NET **V4 liga checksums CRC + aws-chunked por padrão**; a tentativa anterior (`DisablePayloadSigning = ForcePathStyle`) exige HTTPS na V4 e não serve para MinIO em http.
Correção (documentada pela própria AWS, issue aws-sdk-net#3610): remover `DisablePayloadSigning` do `PutObjectRequest` e setar `RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED` no `AmazonS3Config`. Isso mantém SigV4 (funciona em http) e desliga o checksum extra que o MinIO rejeita.
Arquivos: `S3ImageStorage.cs` (remove DisablePayloadSigning + nota), `InfrastructureServiceCollectionExtensions.cs` (+ WHEN_REQUIRED no config).

## Teste (B) com teeth (correção na Task 5)
O teste (B) original usava `ConfigureAppConfiguration(AddInMemoryCollection(Storage:...))`, mas o `appsettings.json` base (R2 placeholder) vencia a precedência — o host subia com ServiceURL R2 inválida e o teste **mascarava** o erro como skip suave ("passed"). Reescrito para registrar o `S3ImageStorage` REAL contra o MinIO via `ConfigureTestServices` (RemoveAll<IImageStorage>+RemoveAll<IAmazonS3>+AddSingleton AmazonS3Client com o mesmo config da produção). Agora: skip suave SÓ quando a porta 9000 não responde; com MinIO up, valida 201 + URL contendo localhost:9000/gatums-imagens e FALHA se algo quebrar. Renomeado para `Admin_Jpeg_MinioReal_RetornaCreated`.

## Verificação final
- `dotnet build GatumsGourmet.sln` → 0 erros.
- Unit: 160 passed.
- Integração UploadImagemEndpointTests: **5/5 passed** (inclui o (B) real contra MinIO). Objeto confirmado no bucket via `mc ls`.
