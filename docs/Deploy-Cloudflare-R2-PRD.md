# Configurar o Cloudflare R2 para Produção (PRD)

Passo a passo para preparar o storage de imagens em **produção** usando
**Cloudflare R2** (S3-compatível, tier gratuito com 10 GB + egress grátis).

O storage da API é agnóstico (mesmo código do MinIO local e do Supabase em HML);
só mudam os valores de configuração. Em PRD, o ambiente é `Production`, então a
API lê a seção `Storage` do `appsettings.json` **base**, com os valores sensíveis
sobrescritos por **variáveis de ambiente** no host.

> **Onde PRD roda:** VPS Locaweb, via Docker Compose
> (`docker-compose.production.yml`), com deploy automático pelo GitHub Actions
> (`.github/workflows/deploy.yml`) no push para `main`. As variáveis de ambiente
> chegam ao container pela cadeia **GitHub Secrets → `.env` na VPS → compose**
> (detalhado no Passo 5).

> **Pré-requisito:** uma conta Cloudflare (gratuita) com R2 habilitado. R2 pede um
> cartão para ativar, mas o tier gratuito não cobra dentro dos limites.

---

## Resumo do que a API espera

| Config (`Storage:*`) | Env var no host | Valor em PRD (R2) |
|----------------------|-----------------|-------------------|
| `ServiceUrl` | `Storage__ServiceUrl` | `https://<ACCOUNT_ID>.r2.cloudflarestorage.com` |
| `Bucket` | `Storage__Bucket` | `gatums-imagens` |
| `Region` | `Storage__Region` | `auto` |
| `AccessKey` | `Storage__AccessKey` | Access Key ID do R2 API Token |
| `SecretKey` | `Storage__SecretKey` | Secret Access Key do R2 API Token |
| `PublicBaseUrl` | `Storage__PublicBaseUrl` | URL **pública** do bucket (r2.dev ou domínio custom) — **não** o endpoint S3 |
| `ForcePathStyle` | `Storage__ForcePathStyle` | `true` |
| `MaxFileSizeMB` | `Storage__MaxFileSizeMB` | `5` (ou o limite desejado) |

> **Atenção ao `ServiceUrl` vs `PublicBaseUrl`** — são DOIS endpoints diferentes:
> - `ServiceUrl` = endpoint **S3** (onde a API grava): `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`. **Privado**, exige credencial.
> - `PublicBaseUrl` = base da URL **pública** (onde o navegador lê a imagem via `<img src>`): o domínio `r2.dev` ou um domínio customizado. **Não** use o `r2.cloudflarestorage.com` aqui — ele não serve conteúdo público.
> Foi exatamente a confusão entre esses dois que quebrou o HML; não repita em PRD.

---

## Passo 1 — Criar o bucket R2

1. Painel Cloudflare → **R2** (menu lateral) → **Create bucket**.
2. Nome: **`gatums-imagens`** (exatamente esse; é o que a API usa).
3. Location: **Automatic** (ou a região mais próxima do público; R2 ignora `Region` do SDK, mas use `auto` na config).
4. **Create bucket**.

---

## Passo 2 — Pegar o Account ID (para o `ServiceUrl`)

1. No painel do R2, clique no bucket (ou na página inicial do R2).
2. Em **Settings** do bucket ou no canto do painel R2, copie o **Account ID** (string hex de 32 caracteres).
3. Monte o endpoint S3:
   ```
   https://<ACCOUNT_ID>.r2.cloudflarestorage.com
   ```
   Esse é o valor de `Storage__ServiceUrl`. **Sem** o nome do bucket no final (o `ForcePathStyle=true` adiciona o bucket ao path automaticamente).

> O painel do R2 também mostra o endpoint pronto em **bucket → Settings → S3 API**.

---

## Passo 3 — Criar as credenciais S3 (R2 API Token)

1. Painel R2 → **Manage R2 API Tokens** (ou **R2 → Overview → API → Create API Token**).
2. **Create API Token**.
3. Permissões: **Object Read & Write** (precisa escrever para upload e, se usar a limpeza de órfãs, ler/deletar).
4. Escopo: pode restringir ao bucket **`gatums-imagens`** (recomendado — menor privilégio).
5. **Create API Token**.
6. O Cloudflare mostra **uma vez só**:
   - **Access Key ID** → vai em `Storage__AccessKey`.
   - **Secret Access Key** → vai em `Storage__SecretKey`.
   Copie os dois agora (o Secret não é exibido de novo).

---

## Passo 4 — Habilitar o acesso público (para o `PublicBaseUrl`)

As URLs que a API devolve precisam ser **lidas publicamente** pelo navegador.
Escolha UMA das opções:

### Opção A — Public Development URL (r2.dev) — mais rápido, bom para validar

1. No bucket → **Settings** → seção **Public access** → **Public Development URL**.
   (A tela diz algo como *"The public development URL is disabled for this bucket"*.)
2. Clique em **Enable** e confirme (o Cloudflare pede para digitar `allow`).
3. O Cloudflare **gera automaticamente** um subdomínio — você não escolhe o nome.
   Ele aparece no formato:
   ```
   https://pub-<hash>.r2.dev
   ```
4. Copie essa URL e **acrescente o nome do bucket** no final para montar o
   `Storage__PublicBaseUrl`:
   ```
   https://pub-<hash>.r2.dev/gatums-imagens
   ```
   (a API acrescenta `/produtos/<arquivo>` no fim ao devolver a URL da imagem).

> **Esclarecimento:** "Public Development URL" e "R2.dev subdomain" são a mesma
> coisa — o subdomínio `pub-<hash>.r2.dev` é gerado pelo Cloudflare ao habilitar;
> não há nome para digitar.
>
> **Nota:** o r2.dev tem **rate limit** e a Cloudflare não recomenda para tráfego
> alto de produção. Serve para validar o fluxo e volumes pequenos. Para PRD de
> verdade, prefira a Opção B (domínio customizado) — basta trocar o
> `Storage__PublicBaseUrl` depois, sem mexer em código.

### Opção B — Domínio customizado (recomendado para PRD real)

1. O domínio (ex.: `gatumsgourmet.com.br`) precisa estar no Cloudflare (DNS gerenciado lá).
2. No bucket → **Settings** → **Public access** → **Custom Domains** → **Connect Domain**.
3. Informe um subdomínio, ex.: `cdn.gatumsgourmet.com.br` ou `imagens.gatumsgourmet.com.br`.
4. O Cloudflare cria o registro DNS e provisiona o certificado (alguns minutos).
5. O `Storage__PublicBaseUrl` fica:
   ```
   https://cdn.gatumsgourmet.com.br/gatums-imagens
   ```

> Com domínio customizado você ganha cache da CDN do Cloudflare de graça e sem o
> rate limit do r2.dev. É a opção certa para PRD.

---

## Passo 5 — Configurar as variáveis em PRD (VPS Locaweb)

**Produção roda na VPS Locaweb** via Docker Compose (`docker-compose.production.yml`),
com deploy automático pelo GitHub Actions (`.github/workflows/deploy.yml`) no push
para `main`. Em Production, o `ASPNETCORE_ENVIRONMENT=Production` faz a API usar o
`appsettings.json` base; os valores sensíveis vêm por **variável de ambiente**.

Os valores do R2 percorrem **três lugares** até chegar ao container. Hoje esse
caminho já existe para `DB_*` e `JWT_*` — é preciso **adicionar os `Storage__*`**
nos três:

```
GitHub Secrets  ──►  deploy.yml (escreve o .env na VPS)  ──►  docker-compose.production.yml (injeta no container)
```

### 5.1 — GitHub Secrets (onde o valor sensível fica guardado)

Repositório → **Settings → Secrets and variables → Actions → New repository secret**.
Crie estes secrets:

| Secret | Valor (de onde vem) |
|--------|---------------------|
| `STORAGE_SERVICE_URL` | `https://<ACCOUNT_ID>.r2.cloudflarestorage.com` (Passo 2) |
| `STORAGE_ACCESS_KEY` | Access Key ID do R2 API Token (Passo 3) |
| `STORAGE_SECRET_KEY` | Secret Access Key do R2 API Token (Passo 3) |
| `STORAGE_PUBLIC_BASE_URL` | URL pública + bucket, ex.: `https://pub-<hash>.r2.dev/gatums-imagens` (Passo 4) |

> `Bucket`, `Region`, `ForcePathStyle` e `MaxFileSizeMB` **não precisam** virar
> secret — são valores não-sensíveis que podem ir fixos no compose (ver 5.3).

### 5.2 — deploy.yml (passar os secrets para o `.env` da VPS)

No job **deploy**, em dois lugares:

**(a)** No bloco `env:` do job, mapeie os secrets:
```yaml
      STORAGE_SERVICE_URL:     ${{ secrets.STORAGE_SERVICE_URL }}
      STORAGE_ACCESS_KEY:      ${{ secrets.STORAGE_ACCESS_KEY }}
      STORAGE_SECRET_KEY:      ${{ secrets.STORAGE_SECRET_KEY }}
      STORAGE_PUBLIC_BASE_URL: ${{ secrets.STORAGE_PUBLIC_BASE_URL }}
```

**(b)** No step **"Create .env on VPS from GitHub Secrets"**, acrescente as linhas:
```bash
          printf 'STORAGE_SERVICE_URL=%s\n'     "$STORAGE_SERVICE_URL"     >> /tmp/.env
          printf 'STORAGE_ACCESS_KEY=%s\n'      "$STORAGE_ACCESS_KEY"      >> /tmp/.env
          printf 'STORAGE_SECRET_KEY=%s\n'      "$STORAGE_SECRET_KEY"      >> /tmp/.env
          printf 'STORAGE_PUBLIC_BASE_URL=%s\n' "$STORAGE_PUBLIC_BASE_URL" >> /tmp/.env
```

### 5.3 — docker-compose.production.yml (injetar no container `gatums-api`)

No serviço `gatums-api`, bloco `environment:`, adicione (duplo underscore no nome
da variável do .NET; o `${...}` lê do `.env`):
```yaml
      Storage__ServiceUrl:    "${STORAGE_SERVICE_URL}"
      Storage__AccessKey:     "${STORAGE_ACCESS_KEY}"
      Storage__SecretKey:     "${STORAGE_SECRET_KEY}"
      Storage__PublicBaseUrl: "${STORAGE_PUBLIC_BASE_URL}"
      Storage__Bucket:        "gatums-imagens"
      Storage__Region:        "auto"
      Storage__ForcePathStyle: "true"
      Storage__MaxFileSizeMB: "5"
```

> Resultado: no container, o .NET lê `Storage:ServiceUrl` etc. e o gate do storage
> detecta como **configurado** → upload usa o R2 de verdade.

### CORS de PRD (o front oficial)

O front de produção é `https://app.gatumsgourmet.com.br`, já presente no
`appsettings.json` base (`Cors:AllowedOrigins`). Como o compose de produção não
sobrescreve CORS, nada a fazer — a menos que o domínio do front mude, aí adicione
`Cors__AllowedOrigins__0` no compose/`.env` da mesma forma.

---

## Passo 6 — Validar após o deploy

1. **Dispare o deploy:** faça push/merge para `main` (o GitHub Actions builda e
   sobe na VPS). Ou rode o workflow manualmente (**Actions → CI/CD → Run workflow**).
2. **Confira o `.env` na VPS** (SSH): as linhas `STORAGE_*` foram gravadas em
   `/opt/gatums-gourmet-api/.env`.
3. **No log do container**, confira a linha de diagnóstico (logo no startup):
   ```
   docker logs gatums-api --tail 50 | grep "[Storage]"
   ```
   Esperado:
   ```
   [Storage] Diagnóstico no startup: ambiente=Production | configurado=true |
   ServiceUrlPreenchida=true | Bucket='gatums-imagens' | Region='auto' |
   PublicBaseUrlPreenchida=true | AccessKeyPreenchida=true | SecretKeyPreenchida=true | ...
   ```
   - Se `configurado=false` → a env var `Storage__ServiceUrl` não chegou ao
     container (verifique os 3 lugares do Passo 5: secret existe? deploy.yml
     escreveu no .env? compose mapeou?).
4. **Teste o upload:** `POST /api/v1/imagens` (multipart, campo `arquivo`, com um
   JPEG/PNG/WebP), autenticado com permissão `produtos:escrever`.
   - Esperado: **201** com `{ "url": "https://pub-<hash>.r2.dev/gatums-imagens/produtos/<guid>.jpg" }`.
5. **Abra a `url` retornada no navegador** — deve exibir a imagem (confirma que o
   `PublicBaseUrl` está público e correto).

---

## Checklist de erros comuns (aprendidos no HML)

| Sintoma | Causa provável | Correção |
|---------|----------------|----------|
| Upload → **400** "storage não configurado" | `Storage__ServiceUrl` vazio/não chegou ao container | Conferir os 3 lugares do Passo 5 (GitHub Secret → `.env` via deploy.yml → `environment:` do compose). Em HML/Render era o Environment Group não-linkado. |
| Upload → **500 / NotFound** | `ServiceUrl` apontando para lugar errado (ex.: a própria API) **ou** bucket inexistente | `ServiceUrl` = `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`; criar o bucket `gatums-imagens` |
| Upload → **500 / 403 Forbidden** | Access/Secret Key erradas ou sem permissão de escrita | Gerar R2 API Token com **Object Read & Write** |
| Upload 201, mas a imagem **não abre** no `<img>` | `PublicBaseUrl` apontando para o endpoint S3 (privado) | Usar a URL pública (r2.dev ou domínio custom), não `r2.cloudflarestorage.com` |
| Limpeza de órfãs loga erro de listagem | Provedor não suporta ListObjectsV2 | O código já faz fallback para ListObjects v1; R2 suporta V2, então não deve ocorrer |

---

## Notas sobre a limpeza de imagens órfãs em PRD

- O `OrphanImageCleanupService` roda a cada `Storage:OrphanCleanupIntervalHours`
  (default 24h) e remove do bucket objetos sob `produtos/` não referenciados por
  nenhum produto, com carência até o início do dia.
- O R2 **suporta** `ListObjectsV2` e `DeleteObject`, então a limpeza funciona.
- Se preferir desligar em PRD por enquanto, há a flag `Storage:OrphanCleanupEnabled`
  (default `true`). Para desligar: `Storage__OrphanCleanupEnabled=false`.

---

## Mapa: de onde vem cada valor → onde ele vai

Ligação ponta a ponta de cada valor, do Cloudflare até o container em PRD:

| Valor | De onde pegar (Cloudflare) | GitHub Secret | Variável no container (compose) |
|-------|----------------------------|---------------|---------------------------------|
| Endpoint S3 | Account ID (Passo 2) → `https://<ACCOUNT_ID>.r2.cloudflarestorage.com` | `STORAGE_SERVICE_URL` | `Storage__ServiceUrl` |
| Access Key ID | R2 API Token (Passo 3) | `STORAGE_ACCESS_KEY` | `Storage__AccessKey` |
| Secret Access Key | R2 API Token (Passo 3) | `STORAGE_SECRET_KEY` | `Storage__SecretKey` |
| URL pública base | Public Development URL ou domínio custom (Passo 4) + `/gatums-imagens` | `STORAGE_PUBLIC_BASE_URL` | `Storage__PublicBaseUrl` |
| Bucket | fixo | — (não é secret) | `Storage__Bucket` = `gatums-imagens` |
| Region | fixo | — | `Storage__Region` = `auto` |
| Path style | fixo | — | `Storage__ForcePathStyle` = `true` |
| Tamanho máx | fixo | — | `Storage__MaxFileSizeMB` = `5` |

Fluxo: **Cloudflare** (origem do valor) → **GitHub Secrets** (guarda o sensível) →
**deploy.yml** (escreve no `.env` da VPS) → **docker-compose.production.yml**
(injeta como env var no container) → **API** (lê `Storage:*`).

## Referência rápida dos valores (template para preencher)

```
# Cloudflare R2 — PRD
Account ID.......: ____________________________________
Bucket...........: gatums-imagens
Access Key ID....: ____________________________________
Secret Access Key: ____________________________________  (copiar na criação; não é reexibido)
Endpoint S3......: https://<ACCOUNT_ID>.r2.cloudflarestorage.com
URL pública base.: https://pub-<hash>.r2.dev/gatums-imagens   (ou domínio custom: https://cdn.gatumsgourmet.com.br/gatums-imagens)

# GitHub Secrets a criar (Settings → Secrets and variables → Actions):
STORAGE_SERVICE_URL......: https://<ACCOUNT_ID>.r2.cloudflarestorage.com
STORAGE_ACCESS_KEY.......: <Access Key ID>
STORAGE_SECRET_KEY.......: <Secret Access Key>
STORAGE_PUBLIC_BASE_URL..: https://pub-<hash>.r2.dev/gatums-imagens
```
