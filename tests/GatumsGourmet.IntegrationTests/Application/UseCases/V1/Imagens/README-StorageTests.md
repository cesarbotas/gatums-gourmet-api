# Testes de Storage — Local (MinIO) e HML (Supabase)

## Estrutura

| Teste | Target | Trait | Quando roda |
|-------|--------|-------|-------------|
| `S3ImageStorageListDeleteTests` | MinIO local | — | Sempre (skip se porta 9000 fechada) |
| `SupabaseStorageTests` | Supabase HML | `Category=HML` `Category=Supabase` | Quando env vars do Supabase configuradas |

## Rodando testes locais (MinIO)

```bash
# 1. Subir PostgreSQL + MinIO
docker compose -f docker/docker-compose.yml up -d

# 2. Rodar apenas os testes locais
dotnet test tests/GatumsGourmet.IntegrationTests --filter "FullyQualifiedName~S3ImageStorageListDeleteTests"
```

## Rodando testes HML (Supabase)

```bash
# 1. Subir PostgreSQL local
docker compose -f docker/docker-compose.yml up -d postgres

# 2. Configurar variáveis de ambiente do Supabase
export SUPABASE_S3_SERVICE_URL=https://dmodnijwbzlemxlxqzvz.storage.supabase.co/storage/v1/s3
export SUPABASE_S3_ACCESS_KEY=5ebc6179cf5ac36fe7b8ad254db1a42b
export SUPABASE_S3_SECRET_KEY=
export SUPABASE_S3_BUCKET=gatums-imagens
export SUPABASE_S3_REGION=us-west-2
export SUPABASE_S3_PUBLIC_BASE_URL=https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/object/public/gatums-imagens

# 3. Rodar apenas os testes do Supabase
dotnet test tests/GatumsGourmet.IntegrationTests --filter "Category=Supabase"
```

## Rodando todos os testes de storage

```bash
# Local + HML (se env vars configuradas)
dotnet test tests/GatumsGourmet.IntegrationTests --filter "FullyQualifiedName~Storage"
```

## O que cada teste valida

### Local (MinIO)
- `UploadListDelete_ContraMinIO_RemoveObjeto`: upload → list → verify → delete → verify gone

### HML (Supabase)
- `Upload_NoSupabase_RetornaUrlComObjectKey`: upload + verifica URL pública acessível
- `UploadDepoisList_DeveEncontrarObjeto`: upload 2 → list → verify encontrados (V2 ou V1 fallback)
- `UploadDepoisDelete_ObjetoRemovido`: upload → verify acessível → delete → verify 404
- `PurgeOrphanFluxo_Completo_NoSupabase`: upload 3 órfãos → list → delete todos → verify limpo

Todos fazem skip suave quando o target não está disponível, sem falhar a suíte.
