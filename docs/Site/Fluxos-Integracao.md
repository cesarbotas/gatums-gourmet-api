# Fluxos de Integração

Fluxos completos ponta a ponta para integrar o site com a API. Cada fluxo lista as chamadas na ordem correta.

## 1. Cliente — Catálogo e Compra (fluxo principal)

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Login   │───▶│ Catálogo │───▶│ Carrinho │───▶│ Checkout │───▶│ Pagamento│
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
                                                                      │
                                                           ┌──────────▼───┐
                                                           │ Ver Pedidos  │
                                                           └──────────────┘
```

### Passo a passo

#### 1.1 — Registro (se ainda não tem conta)
```http
POST /api/v1/auth/register
Content-Type: application/json

{ "nome": "João", "email": "joao@ex.com", "senha": "Senha@123", "telefone": "11999999999" }
```
→ 201 com `{ id, nome, email, role: "cliente" }`

#### 1.2 — Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "joao@ex.com", "senha": "Senha@123" }
```
→ 200 com `{ token, user: { id, nome, email, role } }`

Salvar o `token`. Usar em todas as chamadas seguintes como `Authorization: Bearer {token}`.

#### 1.3 — Carregar categorias (para filtros)
```http
GET /api/v1/produtos/categorias
```
→ 200 com `["Hambúrgueres", "Acompanhamentos", "Bebidas", ...]`

#### 1.4 — Listar produtos (catálogo)
```http
GET /api/v1/produtos?page=1&pageSize=12&disponivel=true
```
→ 200 com `PaginatedResponse<ProdutoSummaryResponse>`

#### 1.5 — Ver detalhes de um produto
```http
GET /api/v1/produtos/{id}
```
→ 200 com `ProdutoDetailResponse` (inclui `itens[]` para combos)

#### 1.6 — Adicionar ao carrinho
```http
POST /api/v1/carrinho/itens
Authorization: Bearer {token}
Content-Type: application/json

{ "produtoId": "{id do produto}", "quantidade": 2 }
```
→ 200 `true`

#### 1.7 — Ver carrinho
```http
GET /api/v1/carrinho
Authorization: Bearer {token}
```
→ 200 com `{ itens: [...], total: 65.80 }`

#### 1.8 — Atualizar quantidade (opcional)
```http
PATCH /api/v1/carrinho/itens/{produtoId}
Authorization: Bearer {token}
Content-Type: application/json

{ "quantidade": 3 }
```

#### 1.9 — Checkout (carrinho → pedido)
```http
POST /api/v1/pedidos/checkout
Authorization: Bearer {token}
Content-Type: application/json

{ "clienteNome": "João" }
```
→ 201 com `{ "pedidoId": "guid" }`

O carrinho é consumido e limpo. O pedido é criado na etapa `criado`.

#### 1.10 — Listar opções de pagamento
```http
GET /api/v1/pagamentos/opcoes
Authorization: Bearer {token}
```
→ 200 com array de `OpcaoPagamentoResponse`

#### 1.11 — Iniciar pagamento
```http
POST /api/v1/pagamentos/iniciar
Authorization: Bearer {token}
Content-Type: application/json

{ "pedidoId": "{pedidoId}", "metodo": 2, "valor": 65.80 }
```
→ 201 com `TransacaoResponse`

> `metodo`: `0` = CartaoDebito, `1` = CartaoCredito, `2` = Pix

#### 1.12a — Confirmar Pix
```http
POST /api/v1/pagamentos/{transacaoId}/pix/confirmar
Authorization: Bearer {token}
```
→ 200 com `TransacaoResponse` (status: `aprovado`)

> O `pixCopiaCola` e `pixExpiraEm` já vêm na resposta do passo 1.11.

#### 1.12b — OU processar cartão
```http
POST /api/v1/pagamentos/{transacaoId}/cartao
Authorization: Bearer {token}
Content-Type: application/json

{ "numero": "4111111111111111", "nome": "JOÃO", "validade": "12/28", "cvv": "123" }
```
→ 200 com `TransacaoResponse`

#### 1.13 — Consultar status
```http
GET /api/v1/pagamentos/{transacaoId}/status
Authorization: Bearer {token}
```
→ 200 com `TransacaoResponse` (status: `pendente` | `aprovado` | `recusado` | `expirado`)

#### 1.14 — Concluir pagamento do pedido
```http
POST /api/v1/pedidos/{pedidoId}/pagamento/concluir
Authorization: Bearer {token}
Content-Type: application/json

{ "transacaoId": "{transacaoId}" }
```
→ 200 `true`

#### 1.15 — Ver meus pedidos
```http
GET /api/v1/pedidos?page=1&pageSize=10
Authorization: Bearer {token}
```
→ 200 com `PaginatedResponse<PedidoResponse>` (cliente vê apenas os próprios)

#### 1.16 — Ver detalhes de um pedido
```http
GET /api/v1/pedidos/{id}
Authorization: Bearer {token}
```
→ 200 com `PedidoResponse` (inclui timeline e pagamento)

---

## 2. Admin — Gestão

### 2.1 — Login admin
```http
POST /api/v1/auth/login
{ "email": "admin@gatums.com", "senha": "Admin@123" }
```

### 2.2 — Gerenciar produtos

Criar:
```http
POST /api/v1/produtos
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "nome": "Combo Família",
  "descricao": "2 hambúrgueres + batata + 2 refris",
  "categoria": "Combos",
  "preco": 89.90,
  "fotoUrl": "https://...",
  "disponivel": true,
  "tipo": 1,
  "itens": [{ "produtoId": "{produtoSimplesId}", "quantidade": 2 }]
}
```
→ 201 `{ "id": "guid" }`

Atualizar:
```http
PUT /api/v1/produtos/{id}
Authorization: Bearer {adminToken}
{ "id": "...", "nome": "...", "descricao": "...", "categoria": "...", "preco": 99.90, "fotoUrl": "...", "disponivel": true }
```

Excluir:
```http
DELETE /api/v1/produtos/{id}
Authorization: Bearer {adminToken}
```

### 2.3 — Gerenciar clientes (CRM)

Criar:
```http
POST /api/v1/clientes
Authorization: Bearer {adminToken}
{
  "nome": "João da Silva",
  "email": "joao@ex.com",
  "telefone": "11999999999",
  "endereco": {
    "logradouro": "Rua das Flores", "numero": "123", "complemento": "Apto 1",
    "bairro": "Centro", "cidade": "São Paulo", "estado": "SP", "cep": "01000000"
  }
}
```

### 2.4 — Gerenciar pedidos (timeline)

A timeline do pedido tem 7 etapas (em ordem):

```
criado → aprovado → pagamento → pagamento_aprovado → envio_pendente → entregue → finalizado
```

| Etapa (int) | Nome | Quem avança |
|-------------|------|-------------|
| 0 | criado | automático (checkout) |
| 1 | aprovado | admin |
| 2 | pagamento | cliente (após pagar) |
| 3 | pagamento_aprovado | admin (após validar transação) |
| 4 | envio_pendente | admin |
| 5 | entregue | admin (exige `entregaTipo`) |
| 6 | finalizado | admin |

Avançar etapa:
```http
POST /api/v1/pedidos/{id}/etapas/avancar
Authorization: Bearer {adminToken}
{ "proximaEtapa": 1, "observacao": "Confirmado", "entregaTipo": null }
```

### 2.5 — Validar pagamento

Antes de avançar para `PagamentoAprovado`, o admin valida a transação:

```http
POST /api/v1/pagamentos/{transacaoId}/validar
Authorization: Bearer {adminToken}
```

### 2.6 — Relatórios

```http
GET /api/v1/relatorios/vendas/resumo
Authorization: Bearer {adminToken}
```

---

## 3. Fluxo de pagamento detalhado

```
                  ┌─────────────┐
                  │  Checkout    │
                  │  (cria pedido)│
                  └──────┬───────┘
                         │ pedidoId
                         ▼
                  ┌─────────────┐
                  │  Iniciar     │  POST /pagamentos/iniciar
                  │  Pagamento   │  { pedidoId, metodo, valor }
                  └──────┬───────┘
                         │ transacaoId
              ┌──────────┴──────────┐
              │                     │
         metodo=Pix            metodo=Cartão
              │                     │
              ▼                     ▼
     ┌──────────────┐    ┌──────────────────┐
     │ Confirmar Pix │    │ Processar Cartão  │
     │ POST .../pix/ │    │ POST .../cartao   │
     │   confirmar   │    │ { numero, cvv... }│
     └──────┬───────┘    └────────┬──────────┘
            │                     │
            └──────────┬──────────┘
                       │ status: aprovado
                       ▼
              ┌──────────────────┐
              │ Concluir Pagamento │  POST /pedidos/{id}/pagamento/concluir
              │  do Pedido        │  { transacaoId }
              └────────┬─────────┘
                       │
                       ▼
              ┌──────────────────┐
              │ Avançar etapa     │  POST /pedidos/{id}/etapas/avancar
              │ (cliente →        │  { proximaEtapa: 2 }  (pagamento)
              │  pagamento)       │
              └────────┬─────────┘
                       │
                       ▼
              ┌──────────────────┐
              │ Admin valida      │  POST /pagamentos/{transacaoId}/validar
              │ transação         │
              └────────┬─────────┘
                       │
                       ▼
              ┌──────────────────┐
              │ Admin avança      │  POST /pedidos/{id}/etapas/avancar
              │ para aprovado     │  { proximaEtapa: 3 }  (pagamento_aprovado)
              └──────────────────┘
```
