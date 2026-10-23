# Deploy na Locaweb — Passo a Passo

Guia completo para colocar a API Gatums Gourmet em produção na VPS Locaweb. O processo é **100% automático via GitHub Action** após a configuração inicial (que se faz apenas uma vez).

## Índice

1. [Visão geral do processo](#1-visão-geral-do-processo)
2. [Pré-requisitos](#2-pré-requisitos)
3. [Passo a passo](#passo-a-passo)
4. [Arquivos e processos ajustados](#arquivos-e-processos-ajustados)
5. [Como o Action funciona](#como-o-action-funciona)
6. [Deploy manual (alternativa)](#deploy-manual-alternativa)
7. [Comandos úteis na VPS](#comandos-úteis-na-vps)
8. [Troubleshooting](#troubleshooting)

---

## 1. Visão geral do processo

```
  GitHub (push main)                         VPS Locaweb
  ┌──────────────┐    rsync via SSH      ┌───────────────────┐
  │  GitHub      │ ──────────────────▶  │  /opt/gatums-    │
  │  Action      │                       │  gourmet-api/    │
  │              │    SSH: docker compose│                   │
  │  1. Build    │ ──────────────────▶  │  ┌─────────────┐ │
  │  2. Test     │                       │  │ gatums-api  │ │
  │  3. rsync    │                       │  │  (.NET 10)  │ │
  │  4. .env     │                       │  └──────┬──────┘ │
  │  5. compose  │                       │         │        │
  │  6. health   │                       │  ┌──────▼──────┐ │
  └──────────────┘                       │  │  gatums-db  │ │
                                          │  │ (PostgreSQL)│ │
                                          │  └─────────────┘ │
                                          └───────────────────┘
```

**O que o Action faz automaticamente:**
1. Compila a solução (`dotnet build`)
2. Roda os testes (`dotnet test`)
3. Sincroniza os arquivos para a VPS via `rsync`
4. Instala Docker na VPS se ainda não estiver instalado
5. Cria o `.env` com as credenciais a partir de GitHub Secrets
6. Sobe os containers com `docker compose`
7. Faz health check na API

**O que você faz manualmente (uma única vez):**
- Provisionar a VPS na Locaweb
- Gerar e configurar a chave SSH
- Adicionar os Secrets no GitHub

---

## 2. Pré-requisitos

| Item | Detalhe |
|------|---------|
| VPS Locaweb | Ubuntu 22.04 ou superior (recomendado 1GB RAM mínimo) |
| Acesso SSH à VPS | IP, usuário (geralmente `root`) e senha inicial (ou console web) |
| Repositório GitHub | O código já deve estar no GitHub |
| Docker | **Não precisa instalar manualmente** — o Action instala |

---

## Passo a passo

### Passo 1 — Provisionar VPS na Locaweb

1. Acesse o painel da Locaweb e crie uma **VPS Cloud**.
2. Escolha o sistema **Ubuntu 22.04 LTS** (ou superior).
3. Anote o **IP público** da VPS (ex: `191.252.222.186`).
4. Anote o usuário (geralmente `root`) e a senha inicial.

> Se a Locaweb permitir escolher uma chave SSH durante o provisionamento, você pode pular para o [Passo 3](#passo-3--adicionar-secrets-no-github) e adicionar a chave pública diretamente.

### Passo 2 — Configurar chave SSH

O GitHub Action precisa de uma chave SSH para conectar na VPS sem senha.

**2.1 — Gerar a chave SSH (no seu computador):**

```bash
ssh-keygen -t ed25519 -f ~/.ssh/gatums_deploy -N ""
```

Isso cria dois arquivos:
- `~/.ssh/gatums_deploy` — **chave privada** (vai para o GitHub)
- `~/.ssh/gatums_deploy.pub` — **chave pública** (vai para a VPS)

**2.2 — Copiar a chave pública para a VPS:**

```bash
ssh-copy-id -i ~/.ssh/gatums_deploy.pub root@<IP_DA_VPS>
```

Digite a senha inicial da VPS quando solicitado.

> Se `ssh-copy-id` não funcionar (ou se estiver no Windows), faça manualmente:
> ```bash
> # Copie o conteúdo da chave pública:
> cat ~/.ssh/gatums_deploy.pub
>
> # Conecte na VPS:
> ssh root@<IP_DA_VPS>
>
> # Cole a chave pública:
> mkdir -p ~/.ssh && chmod 700 ~/.ssh
> echo "cole_o_conteudo_aqui" >> ~/.ssh/authorized_keys
> chmod 600 ~/.ssh/authorized_keys
> exit
> ```

**2.3 — Testar a conexão sem senha:**

```bash
ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS> "echo Conectado!"
```

Deve imprimir `Conectado!` sem pedir senha.

**2.4 — Copiar o conteúdo da chave privada** (para colar no GitHub no Passo 3):

```bash
cat ~/.ssh/gatums_deploy
```

Copie **todo** o conteúdo, incluindo `-----BEGIN` e `-----END`.

### Passo 3 — Adicionar Secrets no GitHub

1. No repositório GitHub, vá em **Settings → Secrets and variables → Actions**.
2. Clique em **New repository secret** para cada variável abaixo.

#### Secrets obrigatórios

| Nome | Valor | Exemplo |
|------|-------|---------|
| `VPS_HOST` | IP público da VPS | `191.252.222.186` |
| `VPS_USER` | Usuário SSH | `root` |
| `VPS_SSH_KEY` | Conteúdo da chave privada (colado do Passo 2.4) | `-----BEGIN OPENSSH PRIVATE KEY-----\n...` |
| `DB_PASSWORD` | Senha do PostgreSQL (gere uma forte) | `xK9$mP2nQ8vL4wR7tZ3a` |
| `JWT_SECRET` | Chave JWT (mín 32 caracteres) | `g@tumsG0urmetPr0ducti0nSecretKey!!2026` |

**Como gerar `DB_PASSWORD` e `JWT_SECRET`:**

```bash
# Senha do banco (32 caracteres):
openssl rand -base64 24 | tr -d '/+=' | head -c 32

# Chave JWT (64 caracteres):
openssl rand -base64 48 | tr -d '/+=' | head -c 64
```

#### Secrets opcionais (com defaults)

Se não adicionar, o Action usa o valor padrão:

| Nome | Default | Descrição |
|------|---------|-----------|
| `VPS_DEPLOY_PATH` | `/opt/gatums-gourmet-api` | Caminho dos arquivos na VPS |
| `DB_USER` | `gatums` | Usuário do PostgreSQL |
| `DB_NAME` | `gatums` | Nome do banco de dados |
| `JWT_ISSUER` | `GatumsGourmet` | Emissor do JWT |
| `JWT_AUDIENCE` | `GatumsGourmet.Web` | Audiência do JWT |

> Só adicione os opcionais se quiser valores diferentes dos defaults.

### Passo 4 — Fazer push para a branch `main`

```bash
git add -A
git commit -m "feat: CI/CD pipeline + deploy Locaweb"
git push origin main
```

O GitHub Action dispara automaticamente. Acompanhe em **Actions** no GitHub.

### Passo 5 — Verificar o deploy

Após o Action concluir (verde ✅), teste:

```bash
# Health check (endpoint público, retorna lista de categorias)
curl http://<IP_DA_VPS>/api/v1/produtos/categorias

# Resposta esperada:
# ["Hambúrgueres","Acompanhamentos","Bebidas"]
```

Login do admin (seed automático):
```bash
curl -X POST http://<IP_DA_VPS>/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gatums.com","senha":"Admin@123"}'
```

Deve retornar `{ "token": "...", "user": {...} }`.

> **Importante**: altere a senha do admin em produção. O seed cria `admin@gatums.com / Admin@123` automaticamente.

### Passo 6 (opcional) — Configurar domínio

Se tiver um domínio apontado para a VPS:

1. No DNS, crie um registro **A** apontando o domínio para o IP da VPS.
2. Para HTTPS, instale Nginx + Certbot na VPS:
   ```bash
   ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS>
   apt-get install -y nginx certbot python3-certbot-nginx
   ```

3. Crie a config do Nginx (`/etc/nginx/sites-available/gatums`):
   ```nginx
   server {
       listen 80;
       server_name api.seudominio.com.br;

       location / {
           proxy_pass http://localhost:8080;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

4. Ative e gere o certificado SSL:
   ```bash
   ln -s /etc/nginx/sites-available/gatums /etc/nginx/sites-enabled/
   nginx -t && systemctl reload nginx
   certbot --nginx -d api.seudominio.com.br
   ```

5. Atualize `VITE_API_BASE_URL` no site Lovable para `https://api.seudominio.com.br/api/v1`.

---

## Arquivos e processos ajustados

### Arquivos criados/modificados

| Arquivo | Ação | Descrição |
|---------|------|-----------|
| `.github/workflows/deploy.yml` | **criado** | Pipeline CI/CD: build, test, rsync, .env, docker compose, health check |
| `docker/Dockerfile` | **modificado** | Atualizado de .NET 8 para .NET 10 (todos projetos usam `net10.0`) |
| `docker-compose.production.yml` | **criado** | Compose de produção: API + PostgreSQL com healthcheck e limites de RAM |
| `deploy/deploy-locaweb.sh` | **criado** | Script que roda na VPS: para containers antigos, build, verifica saúde |
| `deploy/setup-vps.sh` | **criado** | Setup da VPS: instala Docker, firewall, swap (chamado pelo Action) |
| `.env.example` | **criado** | Template de variáveis de ambiente (referência, não é usado em prod) |
| `.dockerignore` | **criado** | Exclui .git, docs, tests, bin, obj do build Docker |

### O que cada arquivo faz no pipeline

```
push main
    │
    ▼
┌─────────────────────────────────────────────────────┐
│  Job 1: Build & Test                                 │
│  ├── dotnet restore GatumsGourmet.sln                │
│  ├── dotnet build --configuration Release           │
│  └── dotnet test --configuration Release             │
└──────────────────────┬──────────────────────────────┘
                       │ (se passar)
                       ▼
┌─────────────────────────────────────────────────────┐
│  Job 2: Deploy                                       │
│  1. webfactory/ssh-agent    → carrega chave SSH       │
│  2. ssh-keyscan             → adiciona VPS ao known   │
│  3. mkdir -p /opt/...       → cria pasta na VPS       │
│  4. rsync -avz --delete     → sincroniza arquivos     │
│  5. ssh: setup-vps.sh       → instala Docker se faltar│
│  6. scp .env                → cria .env de Secrets    │
│  7. ssh: deploy-locaweb.sh  → docker compose up       │
│  8. curl health check       → valida API respondendo  │
└─────────────────────────────────────────────────────┘
```

### Processos manuais necessários (apenas 1 vez)

| Processo | Quando | Como |
|----------|--------|------|
| Provisionar VPS na Locaweb | Antes de tudo | Painel Locaweb: criar VPS Ubuntu |
| Gerar chave SSH | Antes de tudo | `ssh-keygen -t ed25519` |
| Copiar chave pública para VPS | Antes de tudo | `ssh-copy-id` ou manual |
| Adicionar Secrets no GitHub | Antes de tudo | Settings → Secrets → Actions |
| Alterar senha do admin | Após primeiro deploy | Via API ou direto no banco |

**Depois disso, todo novo push para `main` faz o deploy automaticamente.**

---

## Como o Action funciona

### Gatilhos

| Evento | O que faz |
|--------|-----------|
| `push` para `main` | Build + Test + **Deploy** |
| `pull_request` para `main` | Build + Test (sem deploy) |
| `workflow_dispatch` (manual) | Build + Test + Deploy |

### Secrets e como são usados

O `.env` na VPS é **criado pelo Action** a partir de GitHub Secrets — não fica no repositório:

```ini
DB_USER=gatums
DB_PASSWORD=***  (de GitHub Secret)
DB_NAME=gatums
JWT_SECRET=***  (de GitHub Secret)
JWT_ISSUER=GatumsGourmet
JWT_AUDIENCE=GatumsGourmet.Web
```

O `docker-compose.production.yml` lê este `.env` e injeta as variáveis nos containers:
- `ConnectionStrings__DefaultConnection` → sobrescreve `appsettings.json`
- `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` → sobrescreve `appsettings.json`

### Portas e networking

| Porta | Serviço | Acesso |
|-------|---------|--------|
| 80 (VPS) → 8080 (container) | API .NET 10 | Público (internet) |
| 5432 (container interno) | PostgreSQL | Rede interna Docker + loopback da VPS |
| 127.0.0.1:5433 → 5432 (container) | PostgreSQL | Apenas loopback da VPS (acesso externo só via túnel SSH) |

A API responde em `http://<IP_DA_VPS>/api/v1/...`.

> Acesso ao banco pelo DBeaver (via túnel SSH) e comandos de validação: ver
> [`docs/Banco/`](Banco/README.md).

---

## Deploy manual (alternativa)

Se quiser fazer deploy manualmente (sem o Action):

```bash
# 1. Sincronizar arquivos
rsync -avz --delete \
  --exclude '.git' --exclude 'bin' --exclude 'obj' \
  --exclude 'docs' --exclude 'tests' --exclude '.env' \
  --exclude '.github' --exclude '.qodo' --exclude '.vscode' \
  -e "ssh -i ~/.ssh/gatums_deploy" \
  ./ root@<IP_DA_VPS>:/opt/gatums-gourmet-api/

# 2. Criar .env na VPS (use .env.example como base)
ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS> \
  "cd /opt/gatums-gourmet-api && cp .env.example .env && nano .env"

# 3. Rodar o deploy
ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS> \
  "cd /opt/gatums-gourmet-api && bash deploy/deploy-locaweb.sh"
```

---

## Comandos úteis na VPS

```bash
# Conectar na VPS
ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS>

# Ver containers rodando
docker ps

# Logs da API (tempo real)
docker logs -f gatums-api

# Logs do PostgreSQL
docker logs -f gatums-db

# Reiniciar containers
cd /opt/gatums-gourmet-api
docker compose -f docker-compose.production.yml restart

# Parar containers
docker compose -f docker-compose.production.yml down

# Rebuild manual (sem o Action)
docker compose -f docker-compose.production.yml --env-file .env up -d --build

# Ver o .env (credenciais)
cat /opt/gatums-gourmet-api/.env

# Espaço em disco
df -h

# Uso de memória
free -h
```

---

## Troubleshooting

### O Action falha em "Setup SSH"

**Causa**: chave SSH incorreta ou VPS inacessível.

**Solução**:
```bash
# Teste a conexão manualmente
ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS> "echo ok"

# Se não conectar:
# 1. Verifique se o IP está correto no Secret VPS_HOST
# 2. Verifique se a chave privada está completa no Secret VPS_SSH_KEY
#    (deve incluir -----BEGIN e -----END)
# 3. Verifique se a chave pública está na VPS:
#    ssh root@<IP_DA_VPS> "cat ~/.ssh/authorized_keys"
```

### O Action falha em "Build & Test"

**Causa**: erro de compilação ou teste falhando.

**Solução**: rode localmente para reproduzir:
```bash
dotnet build GatumsGourmet.sln --configuration Release
dotnet test GatumsGourmet.sln --configuration Release
```

### A API não responde após o deploy

**Causa**: container não subiu ou demora para iniciar.

**Solução**:
```bash
ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS>

# Ver containers
docker ps -a

# Se o gatums-api não está rodando, veja os logs
docker logs gatums-api --tail 50

# Problema comum: conexão com PostgreSQL
# Aguarde o healthcheck do Postgres (10s) e reinicie
docker compose -f /opt/gatums-gourmet-api/docker-compose.production.yml restart
```

### Erro "Connection refused" no banco

**Causa**: PostgreSQL ainda não está pronto quando a API tenta conectar.

**Solução**: o `docker-compose.production.yml` já tem `depends_on: postgres: condition: service_healthy`. Se ainda assim falhar:
```bash
docker compose -f docker-compose.production.yml restart api
```

### Erro de memória (VPS com 512MB)

**Causa**: VPS com pouca RAM.

**Solução**: o `setup-vps.sh` cria swap de 1GB automaticamente. Se ainda assim faltar memória:
```bash
free -h  # verifique swap e RAM
# Aumentar swap:
fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
```

### Como alterar a senha do admin

O seed cria `admin@gatums.com / Admin@123`. Para alterar em produção:

```bash
ssh -i ~/.ssh/gatums_deploy root@<IP_DA_VPS>
docker exec -it gatums-db psql -U gatums -d gatums -c \
  "SELECT \"Email\", \"SenhaHash\" FROM \"Usuarios\" WHERE \"Role\" = 1;"
```

> A senha é armazenada como hash BCrypt. Para trocar, a forma recomendada é via uma rota de alteração de senha (a ser implementada) ou atualizando o hash diretamente no banco.

### Porta 80 já em uso

Se a porta 80 estiver ocupada por outro serviço na VPS:

```bash
# Ver o que está na porta 80
ss -tlnp | grep :80

# Parar o serviço (ex: Apache/Nginx)
systemctl stop apache2  # ou nginx

# Ou trocar a porta da API no docker-compose.production.yml:
# ports:
#   - "8080:8080"   # acessível em http://<IP>:8080
```
