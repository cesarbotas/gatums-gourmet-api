# Configurar o Cloudflare R2 para Produção (PRD)

Passo a passo para preparar o storage de imagens em **produção** usando
**Cloudflare R2** (S3-compatível, tier gratuito com 10 GB + egress grátis).

O storage da API é agnóstico (mesmo código do MinIO local e do Supabase em HML);
só mudam os valores de configuração. Em PRD, o ambiente é `Production`, então a
API lê a seção `Storage` do `appsettings.json` **base**, com os valores sensíveis
sobrescritos por **variáveis de ambiente** no host.

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

### Opção A — Domínio r2.dev (mais rápido, bom para começar)

1. No bucket → **Settings** → seção **Public access** / **R2.dev subdomain**.
2. **Allow Access** (ativa o subdomínio público `https://pub-<hash>.r2.dev`).
3. O painel mostra a URL pública base, algo como:
   ```
   https://pub-<hash>.r2.dev
   ```
4. O `Storage__PublicBaseUrl` fica:
   ```
   https://pub-<hash>.r2.dev/gatums-imagens
   ```
   (base pública + nome do bucket; a API acrescenta `/produtos/<arquivo>` no fim).

> **Nota:** o `r2.dev` tem rate limit e não é recomendado para alto tráfego de
> produção, mas serve para validar e para volumes pequenos. Para produção de
> verdade, use a Opção B.

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

## Passo 5 — Configurar as variáveis de ambiente no host de PRD

Em **Production**, o `ASPNETCORE_ENVIRONMENT=Production` faz a API usar o
`appsettings.json` base. Os valores sensíveis vêm por env var (duplo underscore).

Defina no host de produção (Render/VPS/etc.):

```
ASPNETCORE_ENVIRONMENT=Production
Storage__ServiceUrl=https://<ACCOUNT_ID>.r2.cloudflarestorage.com
Storage__Bucket=gatums-imagens
Storage__Region=auto
Storage__AccessKey=<ACCESS_KEY_ID_DO_R2>
Storage__SecretKey=<SECRET_ACCESS_KEY_DO_R2>
Storage__PublicBaseUrl=https://cdn.gatumsgourmet.com.br/gatums-imagens
Storage__ForcePathStyle=true
Storage__MaxFileSizeMB=5
```

> **Se usar Environment Group no Render:** lembre de **linkar o group ao serviço**
> (foi o que faltou em HML). Variável em group não-linkado não chega ao processo.
> Prefira, para começar, adicionar direto no serviço para eliminar essa variável.

### CORS de PRD (o front oficial)

O front de produção é `https://app.gatumsgourmet.com.br`. Ele já está no
`appsettings.json` base (`Cors:AllowedOrigins`). Se o domínio do front mudar,
ajuste `Cors__AllowedOrigins__0` no host. (CORS não afeta o upload server-side,
só o navegador do front.)

---

## Passo 6 — Validar após o deploy

1. **Redeploy** o serviço de PRD após setar as variáveis.
2. No log de startup, confira a linha de diagnóstico:
   ```
   [Storage] Diagnóstico no startup: ambiente=Production | configurado=true |
   ServiceUrlPreenchida=true | Bucket='gatums-imagens' | Region='auto' |
   PublicBaseUrlPreenchida=true | AccessKeyPreenchida=true | SecretKeyPreenchida=true | ...
   ```
   - Se `configurado=false` → a env var `Storage__ServiceUrl` não chegou (verifique nome/escopo/link do group).
3. **Teste o upload:** `POST /api/v1/imagens` (multipart, campo `arquivo`, com um JPEG/PNG/WebP), autenticado com permissão `produtos:escrever`.
   - Esperado: **201** com `{ "url": "https://cdn.gatumsgourmet.com.br/gatums-imagens/produtos/<guid>.jpg" }`.
4. **Abra a `url` retornada no navegador** — deve exibir a imagem (confirma que o `PublicBaseUrl` está público e correto).

---

## Checklist de erros comuns (aprendidos no HML)

| Sintoma | Causa provável | Correção |
|---------|----------------|----------|
| Upload → **400** "storage não configurado" | `Storage__ServiceUrl` vazio/não chegou | Conferir nome (`__`), valor e, no Render, link do Environment Group ao serviço |
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

## Referência rápida dos valores (template para preencher)

```
# Cloudflare R2 — PRD
Account ID.......: ____________________________________
Bucket...........: gatums-imagens
Access Key ID....: ____________________________________
Secret Access Key: ____________________________________  (copiar na criação; não é reexibido)
Endpoint S3......: https://<ACCOUNT_ID>.r2.cloudflarestorage.com
URL pública base.: https://cdn.gatumsgourmet.com.br/gatums-imagens   (ou https://pub-<hash>.r2.dev/gatums-imagens)
```
