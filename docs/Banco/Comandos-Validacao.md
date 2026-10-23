# Comandos de validação do banco (VPS produção)

Folha de comandos para diagnosticar "não consigo acessar o banco pelo DBeaver".
Rode na ORDEM. Cada passo diz o que o resultado significa.

Dados de referência:
- Container do banco: `gatums-db`
- Porta interna do Postgres: `5432` | publicada no host (após correção): `127.0.0.1:5433`
- Database `gatums` | user `gatums` | senha = `DB_PASSWORD` do `.env`
- Projeto na VPS: `/opt/gatums-gourmet-api`

ATENÇÃO: os comandos abaixo rodam NA VPS (Linux, via SSH). Comandos
`Test-NetConnection` são de PowerShell/Windows e NÃO existem na VPS — use os
equivalentes Linux indicados aqui.

---

## Passo 1 — O container está de pé e a porta está publicada?

```bash
docker ps --filter "name=gatums-db" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

Leitura do resultado (coluna PORTS):
- `127.0.0.1:5433->5432/tcp`  => CORRETO. Porta publicada só no loopback. Siga para o Passo 2.
- `5432/tcp` (sem `->`)        => PROBLEMA. Porta NÃO publicada no host. O DBeaver
                                  não conecta nem via túnel SSH. Va para o Passo 6 (correção).
- `0.0.0.0:5433->5432/tcp`     => PERIGO. Porta exposta para a internet. Trocar por
                                  `127.0.0.1:5433:5432` (ver Passo 6).

Alternativa que mostra só o mapeamento:
```bash
docker port gatums-db
```
Vazio = nada publicado. `5432/tcp -> 127.0.0.1:5433` = publicado no loopback.

---

## Passo 2 — O Postgres está escutando dentro do container?

```bash
docker exec gatums-db ss -lntp 2>/dev/null | grep 5432 || echo "ss indisponivel, tente o pg_isready abaixo"
docker exec gatums-db pg_isready -U gatums -d gatums
```

- `accepting connections` => banco saudável internamente.
- `no response` / erro     => container subiu mas o Postgres não está pronto; ver logs (Passo 5).

---

## Passo 3 — Login e schema (as credenciais estão certas? as tabelas existem?)

```bash
# Lista os bancos
docker exec -it gatums-db psql -U gatums -d gatums -c "\l"

# Lista as tabelas (criadas pelas migrations do EF Core)
docker exec -it gatums-db psql -U gatums -d gatums -c "\dt"

# Conta registros chave (sanidade)
docker exec -it gatums-db psql -U gatums -d gatums -c 'SELECT count(*) AS usuarios FROM "Usuarios";'
docker exec -it gatums-db psql -U gatums -d gatums -c 'SELECT count(*) AS produtos FROM "Produtos";'
```

- Se o `\dt` lista `"Usuarios"`, `"Produtos"`, `"Pedidos"` etc. => banco OK; o problema
  é SÓ de acesso externo (rede/túnel), não de dados nem de credencial.
- `password authentication failed` => senha incorreta. Confira no Passo 4.
- `database "gatums" does not exist` => o banco não foi criado; ver logs (Passo 5).

---

## Passo 4 — Descobrir a senha real do banco

```bash
grep DB_PASSWORD /opt/gatums-gourmet-api/.env
```

Use esse valor exato no campo Password do DBeaver. Sem aspas, sem espaços.

---

## Passo 5 — Logs do container (se algo acima falhou)

```bash
docker logs --tail 50 gatums-db
```

Procure por: `database system is ready to accept connections` (bom) ou mensagens de
`FATAL` / `could not bind` / `address already in use` (conflito de porta).

---

## Passo 6 — Corrigir: publicar a porta no loopback (causa mais comum)

Se o Passo 1 mostrou `5432/tcp` sem mapeamento, o compose subiu sem publicar a porta.
A correção já está no repositório (`docker-compose.production.yml`, serviço `postgres`):

```yaml
    ports:
      - "127.0.0.1:5433:5432"
```

Para aplicar NA VPS, puxe a versão nova e recrie o container:

```bash
cd /opt/gatums-gourmet-api
git pull
docker compose -f docker-compose.production.yml --env-file .env up -d
```

> Observação: adicionar/alterar `ports` exige RECRIAR o container (o `up -d` faz isso
> automaticamente ao detectar a mudança). Um simples `restart` NÃO aplica novo mapeamento
> de porta. Os dados ficam no volume `gatums-pgdata` e são preservados.

Confirme:
```bash
docker ps --filter "name=gatums-db" --format "{{.Names}} {{.Ports}}"
# esperado: gatums-db 127.0.0.1:5433->5432/tcp
```

---

## Passo 7 — Testar a porta publicada a partir da própria VPS

Depois de publicar, confirme que o loopback responde na 5433:

```bash
# Instala o cliente se faltar (Debian/Ubuntu): apt-get install -y netcat-openbsd postgresql-client
nc -zv 127.0.0.1 5433 && echo "porta 5433 OK no loopback"

# Login pela rede (simula o DBeaver, não usa docker exec)
PGPASSWORD="$(grep -oP 'DB_PASSWORD=\K.*' /opt/gatums-gourmet-api/.env)" \
  psql -h 127.0.0.1 -p 5433 -U gatums -d gatums -c "SELECT version();"
```

- `SELECT version()` respondeu => o túnel SSH do DBeaver vai funcionar (ver `Tunel-SSH-DBeaver.md`).
- `connection refused` => a porta ainda não está publicada; revise o Passo 6.

---

## Passo 8 — Do seu Windows: validar que o túnel SSH chega na VPS

No PowerShell da SUA máquina (não na VPS):

```powershell
# A VPS responde no SSH? (equivalente Windows do nc)
Test-NetConnection -ComputerName <IP_DA_VPS> -Port 22
```

`TcpTestSucceeded : True` => o SSH chega; o túnel do DBeaver tem caminho. O acesso ao
banco em si ocorre por dentro desse túnel, então você NUNCA precisa que a 5433 esteja
aberta na internet — e nem deve estar.

---

## Resumo do diagnóstico (árvore de decisão)

```
docker ps mostra 127.0.0.1:5433->5432/tcp ?
├── NÃO  -> Passo 6 (publicar porta + recriar container) -> Passo 7
└── SIM  -> psql local (Passo 7) responde ?
            ├── NÃO -> ver logs (Passo 5) / conflito de porta
            └── SIM -> problema está no DBeaver -> configurar túnel SSH (Tunel-SSH-DBeaver.md)
                       e conferir senha (Passo 4)
```
