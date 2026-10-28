# Postman — Gatums Gourmet API

Coleção e ambiente para testar a API Gatums Gourmet (.NET 10) com **RBAC + refresh token**.
A coleção é **runner-ready**: um fluxo E2E ordenado (admin + cliente) que passa verde no
**Collection Runner** (bom para o dev) e no **Newman** (CLI, para o QA / CI).

## Arquivos

| Arquivo | Descrição |
|---------|-----------|
| `GatumsGourmet.postman_collection.json` | Coleção completa, ordenada como fluxo E2E, com asserts em cada request |
| `GatumsGourmet-Local.environment.json` | Ambiente local (credenciais seed, variáveis) |
| `package.json` | Scripts Newman (relatório CLI / HTML / JUnit) |

## Importar no Postman

1. **Import** → arraste os dois arquivos JSON.
2. Selecione o ambiente **Gatums Gourmet - Local**.
3. Pré-requisito: API rodando em `{{baseUrl}}` (default `http://localhost:5166`) e banco disponível.

## Como os tokens são obtidos (você não precisa copiar nada)

- **No Collection Runner / Newman:** o grupo **0. Setup** roda primeiro e faz login do admin,
  registra+loga o cliente, e salva `accessToken`/`refreshToken`/expiração de cada um. Todos os
  requests seguintes já usam esses tokens (via `{{adminToken}}` / `{{clienteToken}}`).
- **No uso manual (clicando num request avulso):** um **prerequest de coleção** garante o token
  sob demanda — se você clicar direto em um request protegido (ex.: `Me`) sem ter logado, ele faz
  login do admin automaticamente, e renova por refresh se o token estiver perto de expirar.
  Para o cliente, rode o grupo **0. Setup** uma vez (o cliente precisa ser registrado).

> Resumo: **rode o grupo 0. Setup uma vez** (ou a coleção inteira) e todos os endpoints
> protegidos funcionam, sem copiar token manualmente.

## Rodar no Collection Runner (dev)

1. Clique com o botão direito na coleção → **Run collection**.
2. Deixe a ordem padrão (os grupos estão numerados 0→8). Marque "Keep variable values".
3. **Run**. O Runner mostra verde/vermelho por request (status + asserts).

Fluxo coberto pela corrida (ordenado):
`0. Setup (login admin + cliente)` → `1. Catálogo (admin)` → `2. Endereços (CEP)` →
`3. Clientes CRM (admin)` → `4. Compra (cliente)` → `5. Pedidos (admin)` →
`6. RBAC (admin)` → `7. Relatórios (admin)` → `8. Limpeza (admin)`.

A corrida é **repetível**: nomes/emails usam `{{$timestamp}}` e o fluxo de compra limpa o
carrinho no início; a limpeza final remove o produto e a role de teste.

## Rodar via Newman (QA / performance / CI)

Newman roda a mesma coleção pela linha de comando — mais rápido que a UI e com relatório.

**Pré-requisito:** Node.js. Instale o Newman (global ou via o `package.json` desta pasta):

```bash
# opção A: usando o package.json desta pasta
cd docs/Postman
npm install
npm test            # relatório no terminal
npm run test:html   # gera newman-report.html (bom para enviar ao QA)

# opção B: Newman global
npm install -g newman newman-reporter-htmlextra
newman run GatumsGourmet.postman_collection.json \
  -e GatumsGourmet-Local.environment.json \
  -r cli,htmlextra --reporter-htmlextra-export newman-report.html
```

Para enviar ao QA: mande os **dois JSON** (coleção + environment) e, se quiser, o
`newman-report.html` gerado. O QA ajusta `baseUrl` no environment para o alvo dele
(local/homologação) e roda `npm run test:html`.

> `npm run test:ci` gera um `newman-junit.xml` (para pipelines). Integração no GitHub Actions
> pode ser adicionada depois, se quiser.

## Autenticação (resumo)

- Login/refresh devolvem `{ accessToken, refreshToken, expiresIn, user: { id, nome, email, roles[] } }`.
- Access ~15 min; **refresh rotativo** (o antigo é invalidado; reúso revoga a família → 400).
- Header `Authorization: Bearer <accessToken>`. Autorização por **permissão** (o Admin do seed tem todas).
- Roles de seed: **Admin**, **Cliente**, **Gerente** (PascalCase).

## Enums no body JSON

Enviar enums como **inteiros**: `MetodoPagamento` (0 Débito, 1 Crédito, 2 Pix);
`TipoProduto` (0 Simples, 1 Combo); `PedidoEtapa` (0..6); `EntregaTipo` (0 Transporte, 1 Retirada).
Query params aceitam o nome string (case-insensitive).

## Paginação

Listagens usam `pagina`/`tamanho` e respondem com
`{ itens: [], paginaAtual, tamanhoPagina, totalRegistros, totalPaginas, temPaginaAnterior, temProximaPagina }`.

## Observações

- Ajuste `baseUrl` se a API subir em outra porta (ex.: `5080` no Docker local).
- O `newman-report.html` / `newman-junit.xml` são artefatos de execução — não precisam ir para o git.
