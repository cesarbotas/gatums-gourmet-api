# Túnel SSH no DBeaver (passo a passo)

Como o banco de produção só escuta no loopback da VPS (`127.0.0.1:5433`), o DBeaver
precisa entrar por um túnel SSH. O DBeaver abre a conexão SSH até a VPS e, de dentro
dela, alcança `127.0.0.1:5433`.

Pré-requisito: a porta já publicada no host (ver `Diagnostico-Porta-Nao-Exposta.md`).
Confirme na VPS:
```bash
docker ps --filter "name=gatums-db" --format "{{.Names}} {{.Ports}}"
# gatums-db 127.0.0.1:5433->5432/tcp
```

## Criar a conexão

1. DBeaver -> Database -> New Database Connection -> PostgreSQL -> Next.

2. Aba **Main** (endereços vistos DE DENTRO da VPS, pois o túnel termina lá):
   - Host: `127.0.0.1`
   - Port: `5433`   (a porta publicada no host da VPS, NÃO 5432)
   - Database: `gatums`
   - Username: `gatums`
   - Password: valor de `DB_PASSWORD` do `.env` (na VPS: `grep DB_PASSWORD /opt/gatums-gourmet-api/.env`)
   - Marque "Save password" se quiser.

3. Aba **SSH**:
   - Marque **Use SSH Tunnel**.
   - Host/IP: IP da VPS (o mesmo que você usa para SSH)
   - Port: `22`
   - User Name: `root`
   - Authentication Method:
     - **Password**: digite a senha do SSH; ou
     - **Public Key**: aponte para a chave privada usada no deploy
       (ex. `C:\Users\<voce>\.ssh\gatums_deploy`) e informe a passphrase se houver.

4. Clique em **Test Connection**.
   - Primeira vez: o DBeaver baixa o driver do PostgreSQL. Aceite.
   - Resultado esperado: "Connected".

5. **Finish**.

## Entendendo o fluxo (por que Host = 127.0.0.1)

```
Seu Windows (DBeaver)
   │  abre túnel SSH -> <IP_DA_VPS>:22  (autenticado)
   ▼
VPS Locaweb  ──(de dentro da VPS)──► 127.0.0.1:5433 ──► container gatums-db (Postgres:5432)
```

O campo Host/Port da aba Main é resolvido NA PONTA DO TÚNEL (dentro da VPS). Por isso
`127.0.0.1:5433`, e não o IP público. A porta 5433 nunca precisa (nem deve) estar aberta
na internet.

## Erros comuns

| Erro no DBeaver | Causa provável | Ação |
|-----------------|----------------|------|
| `Connection refused` na Main (túnel OK) | porta não publicada no host | `Diagnostico-Porta-Nao-Exposta.md` (recriar container) |
| EOFException / timeout na Main | Port errada (usou 5432) | usar `5433` na aba Main |
| SSH `Auth fail` | senha/chave SSH errada | revisar aba SSH |
| `password authentication failed` | senha do banco errada | conferir `DB_PASSWORD` no `.env` |
| `database "gatums" does not exist` | banco não criado | ver logs do container (`docker logs gatums-db`) |

## Alternativa sem DBeaver (direto na VPS)

Para rodar um script rápido sem configurar túnel:
```bash
# interativo
docker exec -it gatums-db psql -U gatums -d gatums

# executar um arquivo .sql enviado por scp
scp C:\temp\meu-script.sql root@<IP_DA_VPS>:/tmp/meu-script.sql
ssh root@<IP_DA_VPS> 'docker exec -i gatums-db psql -U gatums -d gatums < /tmp/meu-script.sql'
```
