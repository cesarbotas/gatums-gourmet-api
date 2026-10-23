# Acesso ao Banco de Dados (PostgreSQL)

Documentação para acessar e validar o banco PostgreSQL de produção da Gatums Gourmet,
que roda em container na VPS Locaweb (`gatums-db`).

## Princípio de segurança

O Postgres de produção **não fica exposto à internet**. A porta é publicada apenas no
loopback da VPS (`127.0.0.1:5433`), então o único acesso externo é por **túnel SSH**
autenticado (ex.: DBeaver). Isso está configurado no `docker-compose.production.yml`:

```yaml
  postgres:
    ports:
      - "127.0.0.1:5433:5432"
```

## Documentos

| Documento | Quando usar |
|-----------|-------------|
| [`Diagnostico-Porta-Nao-Exposta.md`](Diagnostico-Porta-Nao-Exposta.md) | O `docker ps` mostra `5432/tcp` sem mapeamento e o DBeaver não conecta |
| [`Comandos-Validacao.md`](Comandos-Validacao.md) | Folha de comandos em ordem para validar o acesso, com leitura dos resultados |
| [`Tunel-SSH-DBeaver.md`](Tunel-SSH-DBeaver.md) | Configurar o túnel SSH no DBeaver passo a passo |

## Dados de conexão (via túnel SSH no DBeaver)

| Item | Valor |
|------|-------|
| Aba Main — Host | `127.0.0.1` |
| Aba Main — Port | `5433` |
| Aba Main — Database | `gatums` |
| Aba Main — Username | `gatums` |
| Aba Main — Password | `DB_PASSWORD` do `.env` da VPS |
| Aba SSH — Host | IP da VPS, porta 22, user `root` |

Descobrir a senha na VPS: `grep DB_PASSWORD /opt/gatums-gourmet-api/.env`

## Convenção do schema (EF Core)

- Tabelas/colunas em PascalCase com aspas duplas: `"Usuarios"`, `"Produtos"`, `"Pedidos"`.
- Enums gravados como texto: `Role` = `Admin`/`Cliente`; `Tipo` = `Simples`/`Combo`;
  `StatusAtual` do pedido = `Criado`/`Aprovado`/.../`Finalizado`.
- PK `Id` é `uuid` (`gen_random_uuid()`); `CategoriaId` é `bigint` (long).
