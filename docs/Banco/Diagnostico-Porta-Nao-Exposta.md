# Diagnóstico: "a porta do banco não está exposta"

Documento focado no problema concreto observado: o DBeaver não conecta porque o
container `gatums-db` subiu SEM publicar a porta no host.

## Sintoma observado na VPS

```bash
docker ps -a --filter "ancestor=postgres:16-alpine" \
  --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```
Resultado real:
```
NAMES       STATUS                 PORTS
gatums-db   Up 8 hours (healthy)   5432/tcp
```

A coluna PORTS traz apenas `5432/tcp`, SEM o prefixo `127.0.0.1:5433->`. Isso significa
que o Postgres escuta só dentro da rede Docker. A API (`gatums-api`) fala com ele pela
rede interna `gatums-net`, mas nada fora do container alcança o banco — nem o próprio
host da VPS, nem o túnel SSH do DBeaver.

## Causa raiz

O `docker-compose.production.yml` não tinha bloco `ports` no serviço `postgres`.
Documentação anterior descrevia a porta `127.0.0.1:5433` como se já estivesse aplicada,
mas a alteração nunca tinha chegado ao arquivo versionado. Resultado: todo deploy
recriava o `gatums-db` sem publicar porta.

## Correção (já aplicada no repositório)

No `docker-compose.production.yml`, serviço `postgres`:
```yaml
    ports:
      - "127.0.0.1:5433:5432"
```
Por que `127.0.0.1:` na frente: publica a porta SÓ no loopback da VPS. Sem esse prefixo
(`5433:5432` ou `0.0.0.0:5433:5432`), a porta ficaria aberta para a internet inteira — o
que para um banco de dados é um risco sério. Com o loopback, o único jeito de entrar é
por um túnel SSH autenticado.

Por que `5433` no host e não `5432`: evita conflito com qualquer outro Postgres que já
escute em 5432 na VPS (isso já causou um "address already in use" no passado).

## Aplicar na VPS

```bash
cd /opt/gatums-gourmet-api
git pull
docker compose -f docker-compose.production.yml --env-file .env up -d
```

`up -d` recria o `gatums-db` porque o mapeamento de portas mudou. O volume
`gatums-pgdata` preserva os dados. Um `docker restart gatums-db` NÃO basta — restart não
reavalia o bloco `ports`.

## Validar

```bash
docker ps --filter "name=gatums-db" --format "{{.Names}} {{.Ports}}"
# esperado: gatums-db 127.0.0.1:5433->5432/tcp
```
Depois siga `Comandos-Validacao.md` (Passo 7) para testar o login pela porta publicada, e
`Tunel-SSH-DBeaver.md` para configurar o túnel SSH no DBeaver.

## O que NÃO fazer

- Não publicar como `0.0.0.0:5433:5432` nem abrir a 5433 no firewall da VPS. O banco
  nunca deve ficar acessível pela internet.
- Não mudar a porta interna do container (continua 5432); só o lado do host é 5433.
- Não apagar o volume `gatums-pgdata` ao recriar — isso zera o banco.
