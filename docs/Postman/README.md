# Postman — Gatums Gourmet API

Coleção e ambiente para testar a API Gatums Gourmet (.NET 10) com **RBAC + refresh token**.
A coleção é **runner-ready**: um fluxo E2E ordenado (admin + cliente) que passa verde no
**Collection Runner** (desenvolvimento) e no **Newman** (CLI, para QA / CI).

## Arquivos

| Arquivo | Descrição |
|---------|-----------|
| `GatumsGourmet.postman_collection.json` | Coleção completa (57 endpoints mapeados), ordenada como fluxo E2E, com asserts em cada request |
| `GatumsGourmet-Local.environment.json` | Ambiente local (credenciais seed, variáveis de execução) |
| `package.json` | Scripts Newman (relatório CLI / HTML / JUnit) |
| `README.md` | Guia completo de uso, estrutura dos endpoints e contratos |

## Importar no Postman

1. **Import** → selecione os arquivos `GatumsGourmet.postman_collection.json` e `GatumsGourmet-Local.environment.json`.
2. Selecione o ambiente **Gatums Gourmet - Local**.
3. Pré-requisito: API rodando em `{{baseUrl}}` (padrão `http://localhost:5166`) e banco disponível.

## Como os tokens são obtidos (você não precisa copiar nada)

- **No Collection Runner / Newman:** o grupo **0. Setup** roda primeiro e faz login do admin,
  registra+loga o cliente, e salva `accessToken`, `refreshToken` e expiração de cada um. Todos os
  requests seguintes já usam esses tokens (via `{{adminToken}}` / `{{clienteToken}}`).
- **No uso manual (clicando num request avulso):** um **prerequest de coleção** garante o token
  sob demanda — se você clicar direto em um request protegido (ex.: `Me`) sem ter logado, ele faz
  login do admin automaticamente, e renova por refresh se o token estiver perto de expirar.
  Para o cliente, rode o grupo **0. Setup** uma vez (o cliente precisa ser registrado).
- **Sincronização de escopo:** os scripts salvam variáveis simultaneamente em `pm.environment` e
  `pm.collectionVariables`, garantindo funcionamento tanto com environment ativo quanto sem environment.

> Resumo: **rode o grupo 0. Setup uma vez** (ou a coleção inteira) e todos os endpoints
> protegidos funcionam, sem copiar token manualmente.

## Estrutura da Coleção (Fluxo E2E)

A coleção é organizada em 10 grupos numerados de **0 a 9** cobrindo todas as funcionalidades da API:

| Grupo | Descrição e Endpoints Cobertos |
|---|---|
| **0. Setup (login admin + cliente)** | Autenticação inicial: `POST /auth/login` (admin), `POST /auth/register` (cliente), `POST /auth/login` (cliente), `GET /auth/me`, `POST /auth/refresh`, `POST /auth/logout`, `POST /auth/logout-all`. |
| **1. Catálogo (admin)** | Gestão completa de catálogo: `POST /categorias`, `GET /categorias` (público paginado), `GET /categorias/{id}`, `PUT /categorias/{id}`, `POST /imagens` (upload multipart form-data), `POST /produtos`, `GET /produtos` (público paginado), `GET /produtos/{id}`, `PUT /produtos/{id}`. |
| **2. Endereços (CEP, público)** | Consulta de CEP via ViaCEP: `GET /enderecos/cep/{cep}`. |
| **3. Clientes CRM (admin)** | CRM de clientes: `POST /clientes`, `GET /clientes` (paginado com busca), `PUT /clientes/{id}`, `DELETE /clientes/{id}`. |
| **4. Compra (cliente)** | Jornada do cliente: `DELETE /carrinho`, `POST /carrinho/itens`, `PATCH /carrinho/itens/{produtoId}` (quantidade), `DELETE /carrinho/itens/{produtoId}`, `GET /carrinho`, `GET /pagamentos/opcoes`, `POST /pedidos/checkout`, `POST /pagamentos/iniciar` (Pix), `POST /pagamentos/{id}/pix/confirmar`, `GET /pagamentos/{id}/status`, `POST /pedidos/{id}/pagamento/concluir`, `POST /pagamentos/iniciar` (Cartão), `POST /pagamentos/{id}/cartao`. |
| **5. Pedidos (admin)** | Gestão de pedidos: `GET /pedidos` (paginado), `GET /pedidos/{id}`, `POST /pagamentos/{id}/validar`, `POST /pedidos/{id}/etapas/avancar` (máquina de estados do workflow). |
| **6. RBAC (admin)** | Permissões, Roles e Usuários: `GET /permissions`, `GET /permissions/{id}`, `POST /permissions`, `PUT /permissions/{id}`, `DELETE /permissions/{id}`, `GET /roles`, `GET /roles/{id}`, `POST /roles`, `PUT /roles/{id}`, `PUT /roles/{id}/permissions`, `POST /usuarios`, `GET /usuarios`, `GET /usuarios/{id}`, `PUT /usuarios/{id}/roles`, `PATCH /usuarios/{id}/ativo` (desativação e reativação), `DELETE /usuarios/{id}`. |
| **7. Relatórios (admin)** | Relatórios gerenciais: `GET /relatorios/vendas/resumo`. |
| **8. Manutenção (admin)** | Rotinas administrativas: `POST /auth/maintenance/purge-tokens` (limpeza manual de refresh tokens expirados). |
| **9. Limpeza (admin)** | Teardown para repetibilidade: `DELETE /produtos/{id}`, `DELETE /categorias/{id}`, `DELETE /roles/{id}`. |

## Rodar no Collection Runner (dev)

1. Clique com o botão direito na coleção → **Run collection**.
2. Deixe a ordem padrão (os grupos estão numerados 0→9). Marque "Keep variable values".
3. **Run**. O Runner mostra verde/vermelho por request (status + asserts).

Fluxo coberto pela corrida (ordenado):
`0. Setup` → `1. Catálogo` → `2. Endereços` → `3. Clientes CRM` → `4. Compra` → `5. Pedidos` → `6. RBAC` → `7. Relatórios` → `8. Manutenção` → `9. Limpeza`.

A corrida é **repetível**: nomes, e-mails e códigos usam `{{$timestamp}}`, o fluxo de compra limpa o
carrinho no início e a limpeza final remove o produto, a categoria e a role de teste.

## Rodar via Newman (QA / performance / CI)

Newman roda a mesma coleção pela linha de comando — rápido e com relatório HTML/JUnit.

**Pré-requisito:** Node.js. Instale as dependências desta pasta:

```bash
# Na pasta docs/Postman:
npm install
npm test            # relatório no terminal (CLI)
npm run test:html   # gera newman-report.html (relatório visual para QA)
npm run test:ci     # gera newman-junit.xml (integração contínua / CI)
```

Para rodar com Newman global:
```bash
newman run GatumsGourmet.postman_collection.json \
  -e GatumsGourmet-Local.environment.json \
  -r cli,htmlextra --reporter-htmlextra-export newman-report.html
```

## Autenticação e RBAC (resumo)

- Login/refresh devolvem `{ accessToken, refreshToken, expiresIn, user: { id, nome, email, roles[] } }`.
- Access token (~15 min, configurável em `Jwt:AccessTokenMinutes`); **refresh rotativo** (~7 dias, reúso revoga a família → 400).
- Header `Authorization: Bearer <accessToken>`.
- Autorização por **permissão** (`recurso:acao`). O usuário Admin do seed possui todas as permissões concedidas.
- Roles de seed: **Admin**, **Cliente**, **Gerente** (PascalCase).
- Permissões disponíveis no seed:
  - `produtos:ler`, `produtos:escrever`
  - `categorias:escrever`
  - `clientes:ler`, `clientes:escrever`
  - `pedidos:ler`, `pedidos:avancar`
  - `pagamentos:validar`
  - `permissoes:gerenciar`
  - `roles:gerenciar`
  - `usuarios:gerenciar`
  - `relatorios:ler`

## Enums no body JSON

Enviar enums como **inteiros**:

- **MetodoPagamento:** `0 = CartaoDebito`, `1 = CartaoCredito`, `2 = Pix`
- **TipoProduto:** `0 = Simples`, `1 = Combo`
- **PedidoEtapa:** `0 = Criado`, `1 = Aprovado`, `2 = Pagamento`, `3 = PagamentoAprovado`, `4 = EnvioPendente`, `5 = Entregue`, `6 = Finalizado`
- **EntregaTipo:** `0 = Transporte`, `1 = Retirada`
- **StatusTransacao:** `0 = Pendente`, `1 = Aprovado`, `2 = Recusado`, `3 = Expirado`

> Query params aceitam o nome em string (case-insensitive).

## Workflow do Pedido (Máquina de Estados)

Transições válidas para `POST /pedidos/{id}/etapas/avancar`:

```
Criado (0) ──[Admin]──> Aprovado (1) ──[Cliente (pagar)]──> Pagamento (2)
                                                               │
                                                           [Admin]
                                                               ▼
Finalizado (6) <──[Admin]── Entregue (5) <──[Admin]── EnvioPendente (4) <── PagamentoAprovado (3)
```

- Quando a próxima etapa for `Entregue (5)`, o campo `entregaTipo` é **obrigatório** (`0 = Transporte` ou `1 = Retirada`).

## Paginação

Listagens usam `pagina` e `tamanho` via query params e respondem com envelope paginado:
```json
{
  "itens": [ ... ],
  "paginaAtual": 1,
  "tamanhoPagina": 10,
  "totalRegistros": 42,
  "totalPaginas": 5,
  "temPaginaAnterior": false,
  "temProximaPagina": true
}
```

## Observações

- Ajuste `baseUrl` no environment caso a API suba em outra porta (ex.: `5080` no Docker local, ou URL de homologação).
- O `newman-report.html` e `newman-junit.xml` são artefatos de execução ignorados pelo git.
