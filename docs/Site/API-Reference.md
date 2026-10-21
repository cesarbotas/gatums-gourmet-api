# API Reference — Todos os Endpoints

Base URL: `{API_BASE}/api/v1`

## Auth

### POST /auth/login
- **Auth**: público
- **Body**: `LoginRequest`
  ```json
  { "email": "string", "senha": "string" }
  ```
- **200**: `LoginResponse`
  ```json
  { "token": "string", "user": { "id": "guid", "nome": "string", "email": "string", "role": "admin|cliente" } }
  ```

### POST /auth/register
- **Auth**: público
- **Body**: `RegisterUserRequest`
  ```json
  { "nome": "string", "email": "string", "senha": "string", "telefone": "string|null" }
  ```
- **201**: `RegisterUserResponse`
  ```json
  { "id": "guid", "nome": "string", "email": "string", "role": "cliente" }
  ```

### GET /auth/me
- **Auth**: autenticado (qualquer role)
- **200**: `GetCurrentUserResponse`
  ```json
  { "id": "guid", "nome": "string", "email": "string", "role": "admin|cliente" }
  ```

### POST /auth/logout
- **Auth**: autenticado (qualquer role)
- **200**: corpo vazio (no-op, JWT é stateless)

---

## Produtos

### GET /produtos/categorias
- **Auth**: público
- **200**: array de strings
  ```json
  ["Hambúrgueres", "Acompanhamentos", "Bebidas"]
  ```

### GET /produtos
- **Auth**: público
- **Query params**:

  | Param | Tipo | Default | Descrição |
  |-------|------|---------|-----------|
  | `page` | int | 1 | Página |
  | `pageSize` | int | 10 | Itens por página |
  | `search` | string? | — | Busca no nome/descrição |
  | `categoria` | string? | — | Filtro por categoria |
  | `tipo` | string? | — | `simples` ou `combo` (case-insensitive) |
  | `disponivel` | bool? | — | Filtra disponibilidade |

  Exemplo: `GET /produtos?page=1&pageSize=20&categoria=Hambúrgueres&disponivel=true`

- **200**: `PaginatedResponse<ProdutoSummaryResponse>`
  ```json
  {
    "items": [
      {
        "id": "guid",
        "nome": "Hambúrguer Artesanal",
        "descricao": "Blend 180g, queijo cheddar...",
        "categoria": "Hambúrgueres",
        "preco": 32.90,
        "fotoUrl": "https://...",
        "disponivel": true,
        "tipo": "simples"
      }
    ],
    "total": 25,
    "page": 1,
    "pageSize": 10
  }
  ```

### GET /produtos/{id}
- **Auth**: público
- **Route**: `id` (Guid)
- **200**: `ProdutoDetailResponse`
  ```json
  {
    "id": "guid",
    "nome": "string",
    "descricao": "string",
    "categoria": "string",
    "preco": 89.90,
    "fotoUrl": "string",
    "disponivel": true,
    "tipo": "combo",
    "itens": [
      { "produtoId": "guid", "quantidade": 2 }
    ]
  }
  ```
- **400**: `"Produto não encontrado."`

### POST /produtos
- **Auth**: `AdminOnly`
- **Body**: `CreateProdutoRequest`
  ```json
  {
    "nome": "string",
    "descricao": "string",
    "categoria": "string",
    "preco": 89.90,
    "fotoUrl": "string",
    "disponivel": true,
    "tipo": 1,
    "itens": [
      { "produtoId": "guid", "quantidade": 2 }
    ]
  }
  ```
  > `tipo`: `0` = Simples, `1` = Combo (inteiros). Combo exige `itens[]` não vazio.
- **201**: `CreateProdutoResponse` → `{ "id": "guid" }`

### PUT /produtos/{id}
- **Auth**: `AdminOnly`
- **Route**: `id` (Guid)
- **Body**: `UpdateProdutoRequest`
  ```json
  {
    "id": "guid",
    "nome": "string",
    "descricao": "string",
    "categoria": "string",
    "preco": 94.90,
    "fotoUrl": "string",
    "disponivel": true
  }
  ```
  > O `id` do body é sobrescrito pelo `id` da rota. Não inclui `tipo`/`itens`.
- **200**: `true` (boolean)

### DELETE /produtos/{id}
- **Auth**: `AdminOnly`
- **200**: `true` (boolean)

---

## Clientes (CRM admin)

### GET /clientes
- **Auth**: `AdminOnly`
- **Query params**: `page` (int, default 1), `pageSize` (int, default 10), `search` (string?)
- **200**: `PaginatedResponse<ClienteResponse>`
  ```json
  {
    "items": [
      {
        "id": "guid",
        "nome": "string",
        "email": "string",
        "telefone": "string|null",
        "endereco": {
          "id": "guid",
          "clienteId": "guid",
          "logradouro": "string",
          "numero": "string",
          "complemento": "string|null",
          "bairro": "string",
          "cidade": "string",
          "estado": "string",
          "cep": "string"
        },
        "criadoEm": "2026-10-20T12:00:00Z"
      }
    ],
    "total": 10,
    "page": 1,
    "pageSize": 10
  }
  ```

### POST /clientes
- **Auth**: `AdminOnly`
- **Body**: `CreateClienteRequest`
  ```json
  {
    "nome": "string",
    "email": "string",
    "telefone": "string|null",
    "endereco": {
      "logradouro": "string",
      "numero": "string",
      "complemento": "string|null",
      "bairro": "string",
      "cidade": "string",
      "estado": "SP",
      "cep": "01000000"
    }
  }
  ```
  > `estado` deve ter 2 caracteres. `cep` máx 8 caracteres.
- **201**: `CreateClienteResponse` → `{ "id": "guid" }`

### PUT /clientes/{id}
- **Auth**: `AdminOnly`
- **Body**: `UpdateClienteRequest`
  ```json
  { "id": "guid", "nome": "string", "email": "string", "telefone": "string|null" }
  ```
  > Não inclui `endereco`.
- **200**: `true`

### DELETE /clientes/{id}
- **Auth**: `AdminOnly`
- **200**: `true`

---

## Carrinho (ClienteOnly)

> O `userId` é extraído automaticamente do token. Todos os endpoints abaixo exigem role `cliente`.

### GET /carrinho
- **200**: `CarrinhoResponse`
  ```json
  {
    "itens": [
      {
        "produtoId": "guid",
        "nome": "string",
        "preco": 32.90,
        "fotoUrl": "string",
        "quantidade": 2
      }
    ],
    "total": 65.80
  }
  ```

### POST /carrinho/itens
- **Body**: `AddCarrinhoItemRequest`
  ```json
  { "produtoId": "guid", "quantidade": 2 }
  ```
- **200**: `true`

### PATCH /carrinho/itens/{produtoId}
- **Route**: `produtoId` (Guid)
- **Body**: `UpdateCarrinhoItemQuantidadeRequest`
  ```json
  { "quantidade": 3 }
  ```
- **200**: `true`

### DELETE /carrinho/itens/{produtoId}
- **Route**: `produtoId` (Guid)
- **200**: `true`

### DELETE /carrinho
- **200**: `true` (limpa todo o carrinho)

---

## Pedidos

### POST /pedidos/checkout
- **Auth**: `ClienteOnly`
- **Body**: `{ "clienteNome": "string" }`
- **201**: `CheckoutPedidoResponse` → `{ "pedidoId": "guid" }`
  > Cria o pedido a partir do carrinho atual. Etapa inicial: `criado`.

### GET /pedidos
- **Auth**: autenticado (qualquer role)
- **Query params**: `page` (int), `pageSize` (int)
- **Comportamento**: admin vê todos; cliente vê apenas os próprios (filtro automático por userId do token)
- **200**: `PaginatedResponse<PedidoResponse>` (ver [Modelos-de-Dados.md](Modelos-de-Dados.md#pedidos))

### GET /pedidos/{id}
- **Auth**: autenticado (qualquer role)
- **Comportamento**: admin vê qualquer pedido; cliente recebe 400 se não for dono
- **200**: `PedidoResponse`

### POST /pedidos/{id}/etapas/avancar
- **Auth**: autenticado (qualquer role)
- **Body**: `AvancarPedidoEtapaRequest`
  ```json
  {
    "proximaEtapa": 1,
    "observacao": "string|null",
    "entregaTipo": null
  }
  ```
  > `proximaEtapa` (int): ver [enums](Erros-e-Padroes.md#enums). Actor derivado do role do token (admin → Admin, cliente → Cliente).
  > `entregaTipo` (int|null): necessário quando a etapa é `Entregue` (5).
- **200**: `true`

### POST /pedidos/{id}/pagamento/concluir
- **Auth**: `ClienteOnly`
- **Body**: `ConcluirPagamentoPedidoRequest`
  ```json
  { "transacaoId": "guid" }
  ```
- **200**: `true`

---

## Pagamentos

### GET /pagamentos/opcoes
- **Auth**: `ClienteOnly`
- **200**: array de `OpcaoPagamentoResponse`
  ```json
  [
    { "metodo": "cartao_credito", "label": "Cartão de Crédito", "disponivel": true },
    { "metodo": "cartao_debito", "label": "Cartão de Débito", "disponivel": true },
    { "metodo": "pix", "label": "Pix", "disponivel": true }
  ]
  ```
  > Aviso: `metodo` aqui usa underscore. Em `/iniciar`, o enum espera **inteiro**.

### POST /pagamentos/iniciar
- **Auth**: `ClienteOnly`
- **Header opcional**: `Idempotency-Key: string` (passa mas não é enforceado no mock)
- **Body**: `IniciarPagamentoRequest`
  ```json
  { "pedidoId": "guid", "metodo": 2, "valor": 89.90 }
  ```
  > `metodo` (int): `0` = CartaoDebito, `1` = CartaoCredito, `2` = Pix
- **201**: `TransacaoResponse` (ver [Modelos-de-Dados.md](Modelos-de-Dados.md#pagamentos))
  > Se já existe transação pendente para o pedido, retorna a existente. Para Pix, resposta inclui `pixCopiaCola` e `pixExpiraEm`.

### POST /pagamentos/{transacaoId}/cartao
- **Auth**: `ClienteOnly`
- **Body**: `ProcessarPagamentoCartaoRequest`
  ```json
  {
    "numero": "4111111111111111",
    "nome": "JOÃO CLIENTE",
    "validade": "12/28",
    "cvv": "123"
  }
  ```
- **200**: `TransacaoResponse`

### GET /pagamentos/{transacaoId}/status
- **Auth**: `ClienteOnly`
- **200**: `TransacaoResponse`

### POST /pagamentos/{transacaoId}/pix/confirmar
- **Auth**: `ClienteOnly`
- **Sem body**
- **200**: `TransacaoResponse`

### POST /pagamentos/{transacaoId}/validar
- **Auth**: `AdminOnly`
- **Sem body**
- **200**: `TransacaoResponse`
  > Valida a transação para aprovação do pedido. Chamar antes de avançar para `PagamentoAprovado`.

---

## Relatórios

### GET /relatorios/vendas/resumo
- **Auth**: `AdminOnly`
- **200**: `VendasResumoResponse`
  ```json
  {
    "faturamento": 15420.50,
    "totalPedidos": 87,
    "ticketMedio": 177.01,
    "topProdutos": [
      { "produtoId": "guid", "nome": "string", "quantidade": 45, "total": 1476.00 }
    ],
    "serieDiaria": [
      { "data": "2026-09-21", "faturamento": 520.00, "pedidos": 3 }
    ]
  }
  ```
  > Últimos 30 dias.
