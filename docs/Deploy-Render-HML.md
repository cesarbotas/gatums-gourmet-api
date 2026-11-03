# Deploy HML — Supabase Storage + Render

Guia passo a passo para configurar o upload de imagens no Supabase, testar localmente, e fazer o deploy de homologação no Render.

## Índice

1. [Problema: Forbidden no upload de imagens](#1-problema-forbidden-no-upload-de-imagens)
2. [Configurar o bucket no Supabase](#2-configurar-o-bucket-no-supabase)
3. [Testar localmente apontando para o Supabase HML](#3-testar-localmente-apontando-para-o-supabase-hml)
4. [Deploy no Render](#4-deploy-no-render)
5. [Variáveis de ambiente no Render](#5-variáveis-de-ambiente-no-render)
6. [Fluxo de validação completo](#6-fluxo-de-validação-completo)

---

## 1. Problema: Forbidden no upload de imagens

### Causa raiz (corrigida neste branch)

O `InfrastructureServiceCollectionExtensions.cs` tinha `UseHttp = true` hardcoded. Isso forçava o SDK da AWS a usar HTTP mesmo quando o `ServiceUrl` era HTTPS (Supabase). O Supabase só aceita HTTPS, resultando em erro de conexão/Forbidden.

**Correção aplicada:**
```csharp
// Antes (quebrava Supabase):
UseHttp = true,

// Depois (deriva do protocolo do ServiceUrl):
UseHttp = storageOptions.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
```

- `http://localhost:9000` (MinIO local) → `UseHttp = true` ✅
- `https://...supabase.co/...` → `UseHttp = false` ✅

### Outros ajustes aplicados

| Arquivo | Problema | Correção |
|---------|----------|----------|
| `appsettings.LocalDocker.json` | `PublicBaseUrl` apontava para `/rest/v1/` (API REST) em vez do storage público | Corrigido para `/storage/v1/object/public/gatums-imagens` |
| `appsettings.Release.json` | `ServiceUrl` apontava para a URL do Render em vez do endpoint S3 do Supabase | Corrigido para `https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/s3` |
| `appsettings.Release.json` | `PublicBaseUrl` também apontava para `/rest/v1/` | Corrigido para `/storage/v1/object/public/gatums-imagens` |

---

## 2. Configurar o bucket no Supabase

### 2.1 — Criar o bucket

1. Acesse o [Dashboard do Supabase](https://supabase.com/dashboard).
2. Selecione o projeto (`dmodnijwbzlemxlxqzvz`).
3. Vá em **Storage → New bucket**.
4. Nome: `gatums-imagens`.
5. Marque **Public bucket** (para leitura pública das imagens).
6. Clique em **Create bucket**.

### 2.2 — Configurar políticas de storage (RLS)

O S3 API do Supabase usa a service role key (não passa por RLS), mas o bucket precisa existir e estar acessível. Para garantir leitura pública via URL:

1. Vá em **Storage → Policies**.
2. Selecione o bucket `gatums-imagens`.
3. Adicione as políticas:

| Política | Para | Usando |
|----------|------|--------|
| Allow public read | `SELECT` | `bucket_id = 'gatums-imagens'` |
| Allow authenticated write | `INSERT` | `bucket_id = 'gatums-imagens'` |
| Allow authenticated delete | `DELETE` | `bucket_id = 'gatums-imagens'` |

Ou via SQL no **SQL Editor**:
```sql
-- Leitura pública
CREATE POLICY "gatums-imagens-public-read"
ON storage.objects FOR SELECT
USING (bucket_id = 'gatums-imagens');

-- Escrita autenticada (S3 API usa service role, bypassa RLS, mas garante)
CREATE POLICY "gatums-imagens-auth-write"
ON storage.objects FOR INSERT
WITH CHECK (bucket_id = 'gatums-imagens');

-- Delete autenticado
CREATE POLICY "gatums-imagens-auth-delete"
ON storage.objects FOR DELETE
USING (bucket_id = 'gatums-imagens');
```

### 2.3 — Obter credenciais S3

1. Vá em **Settings → Storage → S3 Connection**.
2. Anote:
   - **S3 Endpoint**: `https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/s3`
   - **Access Key**: `5ebc6179cf5ac36fe7b8ad254db1a42b` (já no `appsettings.LocalDocker.json`)
   - **Secret Key**: `sb_secret_...` (já no `appsettings.LocalDocker.json`)
   - **Region**: `us-west-2`

> As credenciais já estão no `appsettings.LocalDocker.json` para teste local. No Render, use variáveis de ambiente (seção 5).

### 2.4 — URL pública das imagens

As imagens enviadas ficam acessíveis em:
```
https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/object/public/gatums-imagens/produtos/<uuid>.jpg
```

---

## 3. Testar localmente apontando para o Supabase HML

### 3.1 — Pré-requisitos

- Docker rodando (para PostgreSQL local na porta 5433)
- .NET 10 SDK instalado

### 3.2 — Subir o PostgreSQL local

```bash
docker compose -f docker/docker-compose.yml up -d postgres
```

> O MinIO também está no compose, mas não é necessário para este teste (vamos usar o Supabase).

### 3.3 — Rodar a API apontando para o Supabase

O `appsettings.LocalDocker.json` já tem a configuração do Supabase (endpoint, credenciais, bucket). Para usá-lo:

```bash
ASPNETCORE_ENVIRONMENT=LocalDocker dotnet run --project src/GatumsGourmet.Api
```

A API sobe em `http://localhost:5166` com:
- **Banco de dados**: PostgreSQL local (Docker, porta 5433)
- **Storage de imagens**: Supabase HML (S3 API, HTTPS)

### 3.4 — Testar o upload de imagem

```bash
# 1. Login como admin
TOKEN=$(curl -s -X POST http://localhost:5166/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gatums.com","senha":"Admin@123"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# 2. Upload de imagem (crie um arquivo de teste ou use uma imagem real)
echo -n -e '\xff\xd8\xff\xe0\x00\x10JFIF\x00\x01\x01\x00\x00\x01\x00\x01\x00\x00' > teste.jpg

curl -X POST http://localhost:5166/api/v1/imagens \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@teste.jpg"

# Resposta esperada:
# {"url":"https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/object/public/gatums-imagens/produtos/<uuid>.jpg"}

# 3. Verificar se a imagem é acessível
curl -o /dev/null -w "%{http_code}" "<url_retornada>"
# Deve retornar 200
```

### 3.5 — Se ainda houver Forbidden

Verifique:

1. **O bucket existe?** Dashboard → Storage → deve listar `gatums-imagens`.
2. **As credenciais estão corretas?** Settings → Storage → S3 Connection.
3. **O ServiceUrl está correto?** Deve ser `https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/s3` (com `/storage/v1/s3` no final).
4. **O PublicBaseUrl está correto?** Deve ser `https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/object/public/gatums-imagens`.
5. **A região está correta?** Supabase usa `us-west-2` ou `us-east-1` dependendo do projeto.

---

## 4. Deploy no Render

### 4.1 — Criar o serviço no Render

1. Acesse [render.com](https://render.com) e faça login.
2. **New + → Web Service**.
3. Conecte o repositório GitHub `cesarbotas/gatums-gourmet-api`.
4. Selecione o branch `feature/hml-supabase-render` (ou `release` após merge).
5. Configurações:
   - **Runtime**: Docker
   - **Dockerfile Path**: `./docker/Dockerfile`
   - **Health Check Path**: `/api/v1/produtos/categorias`
   - **Plan**: Starter (ou Free para testes)

> Alternativamente, o `render.yaml` na raiz do repo define tudo automaticamente (Blueprint).

### 4.2 — Criar o PostgreSQL no Render (ou usar Supabase)

**Opção A — Render Postgres (recomendado para HML):**
1. **New + → PostgreSQL** no Render.
2. Anote a **Internal Connection String**.
3. Use como `ConnectionStrings__DefaultConnection`.

**Opção B — Supabase PostgreSQL:**
1. Vá em **Settings → Database** no Supabase.
2. Anote a connection string.
3. Use como `ConnectionStrings__DefaultConnection`.

---

## 5. Variáveis de ambiente no Render

No dashboard do Render, em **Environment**, adicione:

### Obrigatórias

| Variável | Valor | Descrição |
|----------|-------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `Release` | Carrega `appsettings.Release.json` |
| `ConnectionStrings__DefaultConnection` | `Host=...;Port=5432;Database=...;Username=...;Password=...` | PostgreSQL (Render ou Supabase) |
| `Jwt__Secret` | (chave segura, mín 32 chars) | `openssl rand -base64 48 \| tr -d '/+=' \| head -c 64` |
| `Storage__AccessKey` | `5ebc6179cf5ac36fe7b8ad254db1a42b` | Supabase S3 Access Key |
| `Storage__SecretKey` | `sb_secret_...` | Supabase S3 Secret Key |

### Já definidas no appsettings.Release.json (não precisam override)

| Variável | Valor no appsettings | Override só se diferente |
|----------|----------------------|------------------------|
| `Jwt__Issuer` | `GatumsGourmet` | — |
| `Jwt__Audience` | `GatumsGourmet.Web` | — |
| `Storage__ServiceUrl` | `https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/s3` | — |
| `Storage__Bucket` | `gatums-imagens` | — |
| `Storage__Region` | `us-east-1` | `us-west-2` se diferente |
| `Storage__ForcePathStyle` | `true` | — |
| `Storage__PublicBaseUrl` | `https://dmodnijwbzlemxlxqzvz.supabase.co/storage/v1/object/public/gatums-imagens` | — |
| `Cors__AllowedOriginPatterns__0` | `https://gatums-gourmet*.vercel.app` | — |

### Opcionais

| Variável | Quando usar |
|----------|-------------|
| `Cors__AllowedOriginPatterns__0` | Se o front estiver em domínio diferente da Vercel |
| `Storage__MaxFileSizeMB` | Override do limite (default 5MB) |

---

## 6. Fluxo de validação completo

### 6.1 — Local (antes do deploy)

```bash
# 1. Subir PostgreSQL local
docker compose -f docker/docker-compose.yml up -d postgres

# 2. Rodar API com Supabase Storage
ASPNETCORE_ENVIRONMENT=LocalDocker dotnet run --project src/GatumsGourmet.Api

# 3. Testar upload (ver seção 3.4)
# 4. Testar build
dotnet build GatumsGourmet.sln --configuration Release

# 5. Rodar testes unitários
dotnet test tests/GatumsGourmet.UnitTests --configuration Release
```

### 6.2 — Render (após deploy)

```bash
# 1. Fazer push do branch
git add -A
git commit -m "fix: Supabase Storage HTTPS + Render HML config"
git push origin feature/hml-supabase-render

# 2. Render fará o build e deploy automaticamente
# 3. Verificar health check:
curl https://gatums-gourmet-api.onrender.com/api/v1/produtos/categorias
# Deve retornar: ["Hambúrgueres","Acompanhamentos","Bebidas"]

# 4. Testar upload no Render:
TOKEN=$(curl -s -X POST https://gatums-gourmet-api.onrender.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gatums.com","senha":"Admin@123"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -X POST https://gatums-gourmet-api.onrender.com/api/v1/imagens \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@teste.jpg"
```

### 6.3 — Merge para release

Após validar HML no Render:

```bash
git checkout release
git merge feature/hml-supabase-render
git push origin release
```

O Render fará o redeploy automaticamente a partir do branch `release`.

---

## Arquivos alterados neste branch

| Arquivo | Ação | Descrição |
|---------|------|-----------|
| `src/GatumsGourmet.Infrastructure/InfrastructureServiceCollectionExtensions.cs` | **corrigido** | `UseHttp` agora deriva do protocolo do ServiceUrl |
| `src/GatumsGourmet.Api/appsettings.LocalDocker.json` | **corrigido** | `PublicBaseUrl` corrigido para Supabase Storage |
| `src/GatumsGourmet.Api/appsettings.Release.json` | **corrigido** | `ServiceUrl` e `PublicBaseUrl` corrigidos para Supabase |
| `render.yaml` | **criado** | Blueprint do Render (infraestrutura como código) |
| `.env.example` | **atualizado** | Todas as variáveis de ambiente necessárias |
| `docs/Deploy-Render-HML.md` | **criado** | Este guia |

## Arquivos que NÃO foram alterados (local Docker)

- `docker/docker-compose.yml` — mantido como estava (PostgreSQL + MinIO para dev local)
- `docker/Dockerfile` — mantido com .NET 10 (necessário, projetos target net10.0)
