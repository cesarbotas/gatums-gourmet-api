# Backend — Venda Presencial

Referência de implementação para o fluxo de venda presencial (`/admin/venda-rapida`).

## Índice
1. [Arquitetura e decisões](#1-arquitetura-e-decisões)
2. [Novos enums do Domain](#2-novos-enums-do-domain)
3. [Mudanças em entidades existentes](#3-mudanças-em-entidades-existentes)
4. [Use cases (Application)](#4-use-cases-application)
5. [Endpoints (Api)](#5-endpoints-api)
6. [Permissão e autorização](#6-permissão-e-autorização)
7. [Máquina de estados do pedido presencial](#7-máquina-de-estados-do-pedido-presencial)
8. [Fluxo completo de chamadas](#8-fluxo-completo-de-chamadas)
9. [Erros esperados](#9-erros-esperados)
10. [Migration](#10-migration)
11. [Arquivos criados/modificados](#11-arquivos-criadosmodificados)

---

## 1. Arquitetura e decisões

| Decisão | Justificativa |
|---------|---------------|
| Endpoint separado `/api/v1/admin/vendas-presenciais` | Semântica diferente do checkout online. O pedido presencial pula Criado/Aprovado e entra direto em `Pagamento`. |
| `OperadorId` extraído do JWT | Nunca confiar no valor do body. O endpoint ignora qualquer `operadorId` no body e usa `user.GetUserId()`. |
| Total calculado no servidor | Frontend envia apenas `produtoId + quantidade`. O preço é lido do banco. |
| `MetodoPagamentoPresencial` novo enum | Adiciona `Dinheiro` que não existe no fluxo online (não mapeia para PSP). |
| `CanalVenda` em `Pedido` | Distingue pedidos online de presenciais. Permite relatórios e filtros por canal. |
| Dinheiro: aprovação imediata | Sem integração com PSP. O servidor aprova e calcula o troco na mesma chamada. |
| Cartão: confirmação manual | Maquininha processa fora da API. Operador confirma via endpoint dedicado. |
| Pix: mesmo provider | Reutiliza `IPixProvider` (MercadoPago/Mock). Polling/webhook existentes continuam funcionando. |

---

## 2. Novos enums do Domain

### `CanalVenda` — `Domain/Enums/Pedidos/CanalVenda.cs`
```csharp
public enum CanalVenda
{
    Online,     // Fluxo cliente (checkout pelo app/site)
    Presencial  // Venda no balcão via /admin/venda-rapida
}
```
Armazenado como string no Postgres. Campo `Canal` adicionado a `Pedido`.

### `MetodoPagamentoPresencial` — `Domain/Enums/Pagamentos/MetodoPagamentoPresencial.cs`
```csharp
public enum MetodoPagamentoPresencial
{
    Dinheiro,       // Espécie — confirmação imediata, sem PSP
    CartaoDebito,   // Maquininha — confirmação manual do operador
    CartaoCredito,  // Maquininha — confirmação manual do operador
    Pix             // QR Code dinâmico — webhook/polling
}
```

### `StatusPagamentoPresencial` — `Domain/Enums/Pagamentos/StatusPagamentoPresencial.cs`
```csharp
public enum StatusPagamentoPresencial
{
    AguardandoConfirmacaoOperador, // Cartão passado na maquininha, aguardando operador
    AguardandoPagamento,           // QR Code Pix exibido, aguardando cliente pagar
    Aprovado,                       // Dinheiro, cartão confirmado, ou Pix aprovado
    Recusado,                       // Cartão recusado pelo operador/maquininha
    Expirado                        // Pix expirou sem pagamento
}
```
> Status mapeados na resposta via `StatusTransacao` existente: `Pendente` = Aguardando; `Aprovado`, `Recusado`, `Expirado` = estados finais.

---

## 3. Mudanças em entidades existentes

### `Pedido` — campo `Canal` adicionado
```csharp
public CanalVenda Canal { get; set; } = CanalVenda.Online; // default preserva comportamento do checkout online
```

### `TransacaoPagamento` — campos `ValorRecebido` e `Troco` adicionados
```csharp
public decimal? ValorRecebido { get; set; } // Espécie paga pelo cliente (somente Dinheiro)
public decimal? Troco { get; set; }          // ValorRecebido - Total (calculado pelo servidor)
```

### `TransacaoResponse` — 2 novos campos opcionais
```csharp
decimal? ValorRecebido = null,
decimal? Troco = null
```
> Esses campos aparecem na resposta apenas para pagamentos em dinheiro.

---

## 4. Use cases (Application)

Todos em `Application/UseCases/V1/Admin/VendaPresencial/`. Cada use case tem seus arquivos separados.

### 4.1 `CriarVendaPresencial`

**Pasta:** `CriarVendaPresencial/`

| Arquivo | Conteúdo |
|---------|---------|
| `ICriarVendaPresencialHandler.cs` | Interface |
| `CriarVendaPresencialHandler.cs` | Implementação |
| `CriarVendaPresencialRequest.cs` | `CriarVendaPresencialRequest` + `CriarVendaPresencialItemRequest` |
| `CriarVendaPresencialResponse.cs` | `CriarVendaPresencialResponse` + `CriarVendaPresencialItemResponse` |

**O que faz:**
1. Recebe lista de itens (`produtoId + quantidade`) e `operadorId` (do JWT).
2. Carrega todos os produtos em batch (`GetByIdsAsync`).
3. Valida: produto existe, `Disponivel = true`, `Estoque >= quantidade`.
4. Calcula `Total = ∑(produto.Preco × quantidade)` — nunca aceita preço do frontend.
5. Cria `Pedido` com `StatusAtual = Pagamento`, `Canal = Presencial`.
6. Registra movimentos de estoque (`TipoMovimentoEstoque.Venda`) e linha de `Venda`.
7. Salva tudo em uma única chamada (`SaveChangesAsync`).

**Request:**
```json
{
  "clienteNome": "João Silva",
  "itens": [
    { "produtoId": "guid", "quantidade": 2 },
    { "produtoId": "guid", "quantidade": 1 }
  ]
}
```
> `operadorId` não entra no body — é extraído do JWT no endpoint.

**Response 201:**
```json
{
  "pedidoId": "guid",
  "total": 89.70,
  "itens": [
    { "produtoId": "guid", "produtoNome": "Burger Clássico", "quantidade": 2, "precoUnitario": 32.90, "subtotal": 65.80 },
    { "produtoId": "guid", "produtoNome": "Refrigerante 350ml", "quantidade": 1, "precoUnitario": 7.90, "subtotal": 7.90 }
  ],
  "canal": "presencial",
  "operadorId": "guid",
  "clienteNome": "João Silva",
  "criadoEm": "2026-01-15T14:32:00Z"
}
```

---

### 4.2 `RegistrarPagamentoPresencial`

**Pasta:** `RegistrarPagamentoPresencial/`

| Arquivo | Conteúdo |
|---------|---------|
| `IRegistrarPagamentoPresencialHandler.cs` | Interface |
| `RegistrarPagamentoPresencialHandler.cs` | Implementação |
| `RegistrarPagamentoPresencialRequest.cs` | Record do request |

**Comportamento por forma de pagamento:**

| Metodo | Comportamento | Status final |
|--------|--------------|-------------|
| `Dinheiro` | Aprovado imediatamente, troco calculado, pedido avança para `PagamentoAprovado` | `aprovado` |
| `CartaoDebito/Credito` | Cria transação `Pendente`, aguarda confirmação manual do operador | `pendente` |
| `Pix` | Gera QR Code via `IPixProvider`, cria transação `Pendente` | `pendente` |

**Request:**
```json
{
  "pedidoId": "guid",
  "metodo": 0,          // MetodoPagamentoPresencial: 0=Dinheiro, 1=CartaoDebito, 2=CartaoCredito, 3=Pix
  "valorRecebido": 100.00,  // obrigatório quando metodo=Dinheiro
  "payerEmail": "joao@ex.com"  // opcional, para notificação Pix
}
```

**Response 201 — Dinheiro:**
```json
{
  "id": "guid",
  "pedidoId": "guid",
  "metodo": "cartaodebito",
  "valor": 89.70,
  "status": "aprovado",
  "valorRecebido": 100.00,
  "troco": 10.30,
  "criadoEm": "...",
  "atualizadoEm": "..."
}
```

**Response 201 — Cartão (maquininha):**
```json
{
  "id": "guid",
  "status": "pendente",
  "metodo": "cartaodebito"
}
```

**Response 201 — Pix:**
```json
{
  "id": "guid",
  "status": "pendente",
  "metodo": "pix",
  "pixCopiaCola": "00020126...",
  "pixQrCodeBase64": "data:image/png;base64,...",
  "pixExpiraEm": "2026-01-15T15:02:00Z"
}
```

---

### 4.3 `ConfirmarPagamentoPresencialCartao`

**Pasta:** `ConfirmarPagamentoPresencialCartao/`

| Arquivo | Conteúdo |
|---------|---------|
| `IConfirmarPagamentoPresencialCartaoHandler.cs` | Interface |
| `ConfirmarPagamentoPresencialCartaoHandler.cs` | Implementação |
| `ConfirmarPagamentoPresencialCartaoRequest.cs` | Record do request |

**O que faz:** Operador confirma ou recusa o cartão após a maquininha processar. Ao aprovar, avança o pedido para `PagamentoAprovado` e cria a `PedidoPagamento`.

**Request:**
```json
{
  "aprovado": true,
  "observacao": "Aprovado na maquininha Stone #0042"
}
```

**Response 200:**
```json
{
  "id": "guid",
  "status": "aprovado",
  "atualizadoEm": "2026-01-15T14:35:00Z"
}
```

---

## 5. Endpoints (Api)

**Pasta:** `Api/Endpoints/V1/Admin/VendaPresencialEndpoints.cs`

| Método | Rota | Permissão | Handler | Descrição |
|--------|------|-----------|---------|-----------|
| POST | `/api/v1/admin/vendas-presenciais` | `vendas_presenciais:criar` | `ICriarVendaPresencialHandler` | Cria pedido presencial |
| POST | `/api/v1/admin/vendas-presenciais/pagamento` | `vendas_presenciais:criar` | `IRegistrarPagamentoPresencialHandler` | Registra pagamento |
| POST | `/api/v1/admin/vendas-presenciais/pagamento/{transacaoId}/confirmar-cartao` | `vendas_presenciais:criar` | `IConfirmarPagamentoPresencialCartaoHandler` | Confirma cartão na maquininha |

**Endpoints existentes reutilizados:**

| Método | Rota | Handler | Quando usar |
|--------|------|---------|-------------|
| GET | `/api/v1/pagamentos/{id}/status` | `IConsultarStatusPagamentoHandler` | Polling do Pix presencial |
| GET | `/api/v1/pedidos/{id}` | `IGetPedidoByIdHandler` | Verificar estado final do pedido |

---

## 6. Permissão e autorização

Nova permissão `vendas_presenciais:criar` seedada automaticamente na migration `AddVendaPresencialCanal`.

| Role | Permissão |
|------|-----------|
| Admin | `vendas_presenciais:criar` ✅ |
| Gerente | `vendas_presenciais:criar` ✅ |
| Cliente | — |

A permissão é incluída nos claims `perm` do JWT ao fazer login — o frontend lê via `GET /auth/me` ou extrai do token.

---

## 7. Máquina de estados do pedido presencial

```
Presencial: Pagamento → PagamentoAprovado → EnvioPendente → Entregue → Finalizado
Online:     Criado → Aprovado → Pagamento → PagamentoAprovado → EnvioPendente → Entregue → Finalizado
```

| Etapa | Gatilho | Actor |
|-------|---------|-------|
| `Pagamento` | Criação via `POST /admin/vendas-presenciais` (automático) | Admin |
| `PagamentoAprovado` | Dinheiro: automático na criação. Cartão: `confirmar-cartao`. Pix: webhook/polling | Admin |
| `EnvioPendente` | Admin avança manualmente (via endpoint existente `/pedidos/{id}/etapas/avancar`) | Admin |
| `Entregue` | Admin avança (exige `entregaTipo`) | Admin |
| `Finalizado` | Admin arquiva | Admin |

> As etapas `Criado` e `Aprovado` são exclusivas do fluxo online. O `PedidoWorkflowService` não foi alterado — a criação presencial cria o pedido diretamente em `Pagamento`, sem passar pelo workflow.

---

## 8. Fluxo completo de chamadas

```
Passo 1 — Cobrar
  POST /admin/vendas-presenciais
  → 201 { pedidoId, total, itens }

Passo 2 — Registrar pagamento
  POST /admin/vendas-presenciais/pagamento { pedidoId, metodo, ... }

  Dinheiro → 201 { status:"aprovado", troco }
           → Fim (Passo 3)

  Cartão  → 201 { status:"pendente" }
           → Operador passa cartão na maquininha
           → POST /admin/vendas-presenciais/pagamento/{id}/confirmar-cartao
           → 200 { status:"aprovado" }
           → Fim (Passo 3)

  Pix     → 201 { pixQrCodeBase64, pixExpiraEm }
           → Frontend exibe QR Code
           → Polling GET /pagamentos/{id}/status (a cada 4s)
           → Quando status == "aprovado" → Fim (Passo 3)

Passo 3 — Concluído
  Exibir total cobrado + troco (dinheiro) + botão "Nova venda"
```

---

## 9. Erros esperados

Todos seguem RFC 7807 (`{ title, status, detail }`). O campo `detail` pode ser exibido diretamente no toast.

| Status | Situação | `detail` (exemplo) |
|--------|----------|---------------------|
| 400 | Produto não encontrado | `"Produto '...' não encontrado."` |
| 400 | Produto indisponível | `"Produto 'X' está indisponível."` |
| 400 | Estoque insuficiente | `"Estoque insuficiente para 'X'. Disponível: 3, solicitado: 5."` |
| 400 | Valor recebido insuficiente | `"Valor recebido (90.00) é menor que o total (95.00)."` |
| 400 | Pedido não é presencial | `"Este pedido não é uma venda presencial."` |
| 400 | Transação já processada | `"Transação já foi processada."` |
| 400 | Confirmação só para cartão | `"Esta confirmação é exclusiva para pagamentos com cartão."` |
| 403 | Sem permissão | *(retornado pelo middleware de auth)* |
| 400 | Nenhum item | `"Nenhum item informado."` |

---

## 10. Migration

**Migration:** `AddVendaPresencialCanal`

Inclui:
- Coluna `Canal` (text, default `""`) na tabela `Pedidos`
- Coluna `ValorRecebido` (numeric 18,2, nullable) na tabela `TransacoesPagamento`
- Coluna `Troco` (numeric 18,2, nullable) na tabela `TransacoesPagamento`
- Seed da permissão `vendas_presenciais:criar` + vínculos para Admin e Gerente

A migration é aplicada automaticamente no startup via `InitializeDatabaseAsync`.

---

## 11. Arquivos criados/modificados

### Criados

| Arquivo | Tipo |
|---------|------|
| `Domain/Enums/Pedidos/CanalVenda.cs` | Enum |
| `Domain/Enums/Pagamentos/MetodoPagamentoPresencial.cs` | Enum |
| `Domain/Enums/Pagamentos/StatusPagamentoPresencial.cs` | Enum (referência — status mapeado em `StatusTransacao`) |
| `Application/UseCases/V1/Admin/VendaPresencial/CriarVendaPresencial/ICriarVendaPresencialHandler.cs` | Interface |
| `Application/UseCases/V1/Admin/VendaPresencial/CriarVendaPresencial/CriarVendaPresencialHandler.cs` | Handler |
| `Application/UseCases/V1/Admin/VendaPresencial/CriarVendaPresencial/CriarVendaPresencialRequest.cs` | Request |
| `Application/UseCases/V1/Admin/VendaPresencial/CriarVendaPresencial/CriarVendaPresencialResponse.cs` | Response |
| `Application/UseCases/V1/Admin/VendaPresencial/RegistrarPagamentoPresencial/IRegistrarPagamentoPresencialHandler.cs` | Interface |
| `Application/UseCases/V1/Admin/VendaPresencial/RegistrarPagamentoPresencial/RegistrarPagamentoPresencialHandler.cs` | Handler |
| `Application/UseCases/V1/Admin/VendaPresencial/RegistrarPagamentoPresencial/RegistrarPagamentoPresencialRequest.cs` | Request |
| `Application/UseCases/V1/Admin/VendaPresencial/ConfirmarPagamentoPresencialCartao/IConfirmarPagamentoPresencialCartaoHandler.cs` | Interface |
| `Application/UseCases/V1/Admin/VendaPresencial/ConfirmarPagamentoPresencialCartao/ConfirmarPagamentoPresencialCartaoHandler.cs` | Handler |
| `Application/UseCases/V1/Admin/VendaPresencial/ConfirmarPagamentoPresencialCartao/ConfirmarPagamentoPresencialCartaoRequest.cs` | Request |
| `Api/Endpoints/V1/Admin/VendaPresencialEndpoints.cs` | Endpoints |
| `Infrastructure/Migrations/..._AddVendaPresencialCanal.cs` | Migration |

### Modificados

| Arquivo | Mudança |
|---------|---------|
| `Domain/Entities/Pedidos/Pedido.cs` | Adicionado `CanalVenda Canal` (default `Online`) |
| `Domain/Entities/Pagamentos/TransacaoPagamento.cs` | Adicionado `ValorRecebido?` e `Troco?` |
| `Application/UseCases/V1/Pagamentos/TransacaoResponse.cs` | Adicionado `ValorRecebido?` e `Troco?` |
| `Infrastructure/Data/Configurations/Pedidos/PedidoConfiguration.cs` | Config de `Canal` como string |
| `Infrastructure/Data/Configurations/Pagamentos/TransacaoPagamentoConfiguration.cs` | Config de `ValorRecebido`/`Troco` como numeric |
| `Application/ApplicationServiceCollectionExtensions.cs` | Registrado 3 novos handlers |
| `Api/Extensions/EndpointRouteBuilderExtensions.cs` | Mapeado grupo `/admin/vendas-presenciais` |
