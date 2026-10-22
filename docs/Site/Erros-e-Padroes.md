# Erros e Padrões

## Formato de erro — ProblemDetails (RFC 9807)

A API retorna erros no formato `application/problem+json`. Todos os erros seguem a mesma estrutura:

```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "Mensagem descritiva do erro."
}
```

| Campo | Descrição |
|-------|-----------|
| `title` | Categoria do erro |
| `status` | HTTP status code |
| `detail` | Mensagem específica |

### Status codes

| Status | Quando | Title |
|--------|--------|-------|
| 400 | Erro de regra de negócio, entidade não encontrada (via Result.Fail) | `"Bad Request"` |
| 400 | Erro de domínio (DomainException) | `"Domain Error"` |
| 401 | Token ausente ou inválido | *(JWT middleware)* |
| 403 | Token válido mas role insuficiente | *(JWT middleware)* |
| 404 | Entidade não encontrada (NotFoundException) | `"Not Found"` |
| 422 | Violação de regra de negócio (BusinessRuleException) | `"Business Rule Violation"` |
| 500 | Erro interno não tratado | `"Internal Server Error"` |

### Exemplos de erro

Credenciais inválidas:
```json
{ "title": "Bad Request", "status": 400, "detail": "Credenciais inválidas." }
```

Produto não encontrado:
```json
{ "title": "Bad Request", "status": 400, "detail": "Produto não encontrado." }
```

E-mail já cadastrado:
```json
{ "title": "Bad Request", "status": 400, "detail": "E-mail já cadastrado." }
```

Acesso negado (cliente tentando ver pedido de outro):
```json
{ "title": "Bad Request", "status": 400, "detail": "Acesso negado." }
```

### Camadas de erro

Há duas camadas que cooperam:

1. **Result pattern** (`ResultExtensions.ToHttpResult`): usado por todos os endpoints. Quando o handler retorna `Result.Fail(msg)`, vira 400 ProblemDetails com `detail = msg`. Em sucesso, retorna 200/201 com o DTO **desembrulhado** (não envolvido em Result).

2. **ExceptionMiddleware**: captura exceções não tratadas. Mapeia `NotFoundException` → 404, `BusinessRuleException` → 422, `DomainException` → 400, outras → 500.

### Validação de campos — ATENÇÃO

FluentValidation está **registrado** (`AddValidatorsFromAssemblyContaining<LoginValidator>()`) mas **nunca invocado** nos handlers. Não há MediatR pipeline behavior nem endpoint filter. Isso significa:

- **Campos inválidos NÃO retornam 400 estruturado** com `errors` por campo.
- Validação de campos (email válido, senha mínima, etc.) deve ser feita **no front**.
- A API pode aceitar dados inválidos e falhar mais tarde com um 400 genérico ou 500.

> O path `ValidationFail → ValidationProblem (400)` é código morto na prática. Se o front enviar JSON malformado, o ASP.NET retorna um 400 default de model binding.

---

## Enums

A API **não** registra `JsonStringEnumConverter`. Isso afeta como enums são enviados e recebidos.

### Em corpo JSON (request) — enviar INTEIROS

| Enum | Membro | Valor int |
|------|--------|-----------|
| `TipoProduto` | Simples | `0` |
| `TipoProduto` | Combo | `1` |
| `MetodoPagamento` | CartaoDebito | `0` |
| `MetodoPagamento` | CartaoCredito | `1` |
| `MetodoPagamento` | Pix | `2` |
| `PedidoEtapa` | Criado | `0` |
| `PedidoEtapa` | Aprovado | `1` |
| `PedidoEtapa` | Pagamento | `2` |
| `PedidoEtapa` | PagamentoAprovado | `3` |
| `PedidoEtapa` | EnvioPendente | `4` |
| `PedidoEtapa` | Entregue | `5` |
| `PedidoEtapa` | Finalizado | `6` |
| `EntregaTipo` | Transporte | `0` |
| `EntregaTipo` | Retirada | `1` |

Exemplo: criar um produto combo:
```json
{ "tipo": 1, "itens": [{ "produtoId": "...", "quantidade": 2 }] }
```

### Em query params — enviar STRINGS (case-insensitive)

O model binder do ASP.NET aceita nomes de enum em query params:

```
GET /produtos?tipo=simples
GET /produtos?tipo=combo
```

### Em responses — strings (convertidas manualmente)

As responses mapeiam enums para strings via `.ToString().ToLower()`:

| Enum | Response | Exemplo |
|------|----------|---------|
| `TipoProduto` | `"simples"` / `"combo"` | `ProdutoSummaryResponse.tipo` |
| `StatusTransacao` | `"pendente"` / `"aprovado"` / `"recusado"` / `"expirado"` | `TransacaoResponse.status` |
| `MetodoPagamento` | `"cartaodebito"` / `"cartaocredito"` / `"pix"` | `TransacaoResponse.metodo` |

> Se o front envia enums como **string** no body (ex.: `"metodo": "Pix"`), a API vai rejeitar com 400. Há duas soluções:
> 1. Registrar `JsonStringEnumConverter` na API (`builder.Services.ConfigureHttpJsonOptions(...)`)
> 2. Enviar inteiros no front

---

## Inconsistência de `metodo` de pagamento

O campo `metodo` aparece em **três formatos diferentes**:

| Onde | Tipo | Formato | Exemplo |
|------|------|---------|---------|
| `GET /pagamentos/opcoes` (response) | string | underscore | `"cartao_credito"` |
| `POST /pagamentos/iniciar` (request) | int (enum) | inteiro | `1` |
| `TransacaoResponse.metodo` (response) | string | lowercase sem underscore | `"cartaocredito"` |

### Mapeamento no front

```typescript
// Opções da API → valor para enviar no /iniciar
const metodoToInt: Record<string, number> = {
  'cartao_credito': 1,
  'cartao_debito': 0,
  'pix': 2,
};

// TransacaoResponse.metodo → label amigável
const metodoToLabel: Record<string, string> = {
  'cartaocredito': 'Cartão de Crédito',
  'cartaodebito': 'Cartão de Débito',
  'pix': 'Pix',
};
```

---

## Cliente ≠ Usuário

Dois conceitos distintos que não devem ser confundidos:

| Conceito | Endpoint | Quem cria | Tem senha? |
|----------|---------|-----------|------------|
| **Usuário** (Auth) | `POST /auth/register` | próprio usuário | sim (login) |
| **Cliente** (CRM) | `POST /clientes` | admin | não |

- O **usuário** é quem faz login e compra (role `cliente`).
- O **cliente** é um registro de diretório gerenciado pelo admin (CRM), com endereço.
- O cadastro de cliente NÃO cria um usuário de login.
- O registro de usuário NÃO cria um cliente no CRM.

---

## Convenções JSON

| Regra | Exemplo |
|-------|---------|
| Property names | camelCase: `{ "precoTotal": 89.90 }` |
| Guid | string: `"3f5e2a1b-..."` |
| DateTime | ISO-8601: `"2026-10-20T12:00:00Z"` |
| Decimal | number: `89.90` |
| Nullable | `null` ou omitido |
| Boolean | `true` / `false` |

### Sucesso — corpo desembrulhado

O `Result<T>` interno tem `{ IsSuccess, Data, Error, ValidationErrors }`, mas **na resposta HTTP só vem `Data`** (sem envelope):

```json
// GET /carrinho → 200
{
  "itens": [...],
  "total": 65.80
}
```

NÃO vem como `{ "isSuccess": true, "data": {...} }`.

### Paginação

```json
{
  "itens": [...],
  "paginaAtual": 1,
  "tamanhoPagina": 10,
  "totalRegistros": 25,
  "totalPaginas": 3,
  "temPaginaAnterior": false,
  "temProximaPagina": true
}
```

- `totalRegistros` é o total de registros encontrados (não de páginas).
- `totalPaginas`, `temPaginaAnterior` e `temProximaPagina` são calculados pela API — o front não precisa derivar nada.
- Query params das listagens: `pagina` (default 1) e `tamanho` (default 10).

---

## Headers importantes

| Header | Valor | Quando |
|--------|-------|-------|
| `Authorization` | `Bearer <token>` | Todas as rotas protegidas |
| `Content-Type` | `application/json` | POST/PUT/PATCH com body |
| `Idempotency-Key` | string (opcional) | `POST /pagamentos/iniciar` (passa mas não é enforceado no mock) |
