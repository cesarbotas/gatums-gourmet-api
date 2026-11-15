# Pagamentos V2 — Pix via Mercado Pago Orders API

- **Data:** 2026-11-11
- **Branch:** `feature/AjustePagamentosPix`
- **Caminho:** arquitetural (nova versão de módulo + novo contrato com PSP)
- **Status:** aguardando revisão do spec

## 1. Contexto e problema

O Pix atual usa `POST /v1/payments` do Mercado Pago (Payments API, modelo legado).
A chamada está sendo rejeitada e o erro do Mercado Pago só aparece no log
(`MercadoPagoPixProvider`). O próprio Mercado Pago recomenda a **Orders API**
(`POST /v1/orders`) para novas integrações no Brasil.

O front-end já consome `/api/v1/pagamentos` e **não deve ser afetado agora**.

## 2. Objetivo e critérios de sucesso

Criar um módulo `V2` de pagamentos, **em paralelo** à V1, que gere o Pix pela Orders
API e processe o webhook/status de ponta a ponta.

Sucesso =
1. `POST /api/v2/pagamentos/pix` cria uma order Pix e devolve o **mesmo
   `TransacaoResponse`** da V1 (copia e cola, QR base64, expiração).
2. Webhook V2 aprova a transação e o pedido quando a order é `processed/accredited`.
3. Erros do Mercado Pago voltam ao cliente como `Result.Fail` com mensagem útil
   (hoje só vão para o log).
4. **Nenhum arquivo/rota/DI da V1 é alterado em comportamento.** A suíte de testes
   existente continua verde sem modificação.

## 3. Fora de escopo

- Qualquer mudança no front.
- Remover/alterar o fluxo V1 (`/v1/payments`, webhook V1).
- Integração real de cartão (continua `MockPaymentGateway`).
- Modo `manual` de order, boleto, reembolso/cancelamento.
- Migration de banco.

## 4. Premissas (confirmadas na documentação do Mercado Pago)

- `POST /v1/orders`, modo `automatic`, headers `Authorization: Bearer` e
  `X-Idempotency-Key`.
- Body Pix: `type: "online"`, `external_reference`, `processing_mode: "automatic"`,
  `total_amount` (string `"50.00"`), `payer.email` (obrigatório),
  `transactions.payments[0]` = `{ amount, payment_method: { id: "pix", type: "bank_transfer" } }`,
  `config.payment.expiration_time` opcional (ISO 8601, 30 min a 30 dias; padrão 24 h).
  `expiration_time` no nível da order **não** deve ser enviado no modo automático.
- Resposta: `id` (`ORD...`), `status`, `status_detail`,
  `transactions.payments[0].{ id (PAY...), payment_method.{ qr_code, qr_code_base64, ticket_url } }`.
  Order criada vem `action_required` / `waiting_transfer`.
- Status finais: `processed/accredited` (pago), `canceled/canceled`,
  `canceled/expired`, `failed`. Pendentes: `action_required/*`, `processing/*`.
- Webhook (tópico `order`): query `data.id=ORD...&type=order`, body com
  `action` (ex.: `order.processed`), `data.status`, `data.status_detail`.
  Header `x-signature: ts=...,v1=...` (HMAC com a assinatura secreta da aplicação).
  Resposta esperada: HTTP 200/201 em até 22 s; reenvio a cada 15 min se falhar.
- Teste: Access Token **de teste**; `payer.email` qualquer e-mail válido;
  `payer.first_name = "APRO"` simula aprovação. **Não é preciso usuário de teste.**

## 5. Design

### 5.1 Rotas — `Api/Endpoints/V2/PagamentosEndpoints.cs`, prefixo `/api/v2/pagamentos`

| Método | Rota | Origem | Auth |
|--------|------|--------|------|
| POST | `/pix` | **novo** (`IniciarPixV2Handler`) | autenticado |
| GET | `/pix/{transacaoId}/status` | **novo** (`ConsultarStatusV2Handler`) | autenticado |
| GET | `/{transacaoId}/status` | **novo** (mesmo handler) | autenticado |
| POST | `/webhook/mercadopago` | **novo** (`ProcessarWebhookOrdersHandler`) | anônimo + assinatura |
| GET | `/opcoes` | reaproveita handler V1 | autenticado |
| POST | `/cartao-credito` | reaproveita handler V1 | autenticado |
| POST | `/cartao-debito` | reaproveita handler V1 | autenticado |

As três últimas são cópias finas (mesma assinatura e handler da V1), para o front
migrar a base de pagamentos de uma vez quando quiser. `EndpointRouteBuilderExtensions`
ganha `MapV2Endpoints()` (o comentário existente já prevê isso).

### 5.2 Camadas

**Domain**
- `Services/Interfaces/IPixOrdersProvider`:
  - `Task<PixOrderResult> CriarOrderAsync(PixOrderRequest, CancellationToken)`
  - `Task<PixOrderStatus?> ConsultarOrderAsync(string orderId, CancellationToken)`
- Tipos: `PixOrderRequest(TransacaoId, Valor, Descricao?, PayerEmail?, PayerNome?)`,
  `PixOrderResult(OrderId, CopiaCola, QrCodeBase64?, TicketUrl?, ExpiraEm)`,
  `PixOrderStatus(Status, StatusDetail)`.
- `IPixProvider` e `PixResult` **não mudam**.

**Infrastructure**
- `MercadoPagoOrdersPixProvider : IPixOrdersProvider` (HttpClient tipado,
  mesma `MercadoPago:BaseUrl`/`AccessToken`). Em falha HTTP lança
  `PixOrdersProviderException` com status e `errors[]` do corpo do Mercado Pago.
- `MockPixOrdersProvider` (usado quando não há `AccessToken`, como no V1).
- Registro de DI **adicional** em `InfrastructureServiceCollectionExtensions`;
  o registro de `IPixProvider` permanece idêntico.
- `x-signature`: `MercadoPagoWebhookSignatureValidator` (HMAC-SHA256, comparação
  em tempo constante, tolerância de `ts`), configurado por
  `MercadoPago:WebhookSecret`. Manifesto conforme a documentação oficial
  (`id:{data.id};request-id:{x-request-id};ts:{ts};`) — **validar contra o exemplo
  oficial num teste**. Sem segredo configurado: aceita e loga aviso apenas em
  `Development`; em outros ambientes rejeita (401).

**Application** — `UseCases/V2/Pagamentos/`
- `IniciarPixV2` (Request = `IniciarPixRequest` da V1 reutilizado; Response =
  `TransacaoResponse`).
- `ConsultarStatusV2`.
- `ProcessarWebhookOrders`.
- `AplicarResultadoPagamentoPixService` (compartilhado por status e webhook):
  encapsula a regra “order paga → transação `Aprovado` + pedido para etapa
  `Pagamento` + timeline”, e “order cancelada/expirada/falha → transação
  `Recusado`”. Só altera transação `Pendente` (idempotente). Evita duplicar a
  lógica hoje copiada entre dois handlers V1.

### 5.3 Dados

Sem migration. Reaproveita `TransacaoPagamento`:
- `OperadoraRef` = `ORD...` (coluna string indexada, comprimento livre).
- `Operadora` = `"mercadopago-orders"`. O `IniciarPixV2` só reaproveita transação
  Pix pendente **com essa operadora**; transações pendentes da V1 (id numérico) não
  são tocadas. O webhook V2 só atua em transações com `OperadoraRef` de order.

### 5.4 Mapeamento de status

| Order (`status`/`status_detail`) | Transação |
|----------------------------------|-----------|
| `processed` / `accredited` | `Aprovado` (+ pedido → `Pagamento`) |
| `canceled` (`canceled`/`expired`), `failed` | `Recusado` |
| `action_required`, `processing`, demais | permanece `Pendente` |

### 5.5 Fluxo `POST /v2/pagamentos/pix`

1. Busca pedido (404 lógico se não existir) e valor (`request.Valor` ou `pedido.Total`).
2. Se há transação Pix `Pendente` da V2 para o pedido, devolve a mesma.
3. Cria `TransacaoPagamento` (Pendente) e chama `CriarOrderAsync`
   (`X-Idempotency-Key` = `transacao.Id`; `external_reference` = `transacao.Id`).
4. Persiste QR/copia-cola/expiração/`OperadoraRef`; devolve `TransacaoResponse`.
5. Se o provider falhar: nada é persistido e retorna `Result.Fail` com a causa.

### 5.6 Webhook `POST /v2/pagamentos/webhook/mercadopago`

1. Valida `x-signature` (401 se inválida).
2. Extrai `data.id` (query ou body); se ausente ou `type != order`, responde 200 e ignora.
3. **Não confia no body**: consulta `ConsultarOrderAsync(data.id)`.
4. Aplica o mapeamento da seção 5.4 via `AplicarResultadoPagamentoPixService`.
5. Sempre 200 para eventos válidos (inclusive transação desconhecida), para não
   gerar reenvios.

### 5.7 Configuração

- `MercadoPago:NotificationUrl` continua sendo URL de webhook da aplicação no painel;
  a V2 usa `…/api/v2/pagamentos/webhook/mercadopago`.
- Novo: `MercadoPago:WebhookSecret` (via variável de ambiente/user-secrets).
- Novo (opcional): `MercadoPago:PixExpirationMinutes` (padrão 30 → ISO `PT30M`).
- Painel do Mercado Pago: ativar notificações do evento **Order**.

## 6. Riscos e pontos em aberto

1. **Um webhook por aplicação?** Não confirmei se o painel aceita URLs distintas para
   `payment` (V1) e `order` (V2) na mesma aplicação. Mitigação se não aceitar: a rota
   V2 do webhook roteia pelo `type` (`order`→V2; `payment`→delega ao handler V1).
   Decidir na implementação após testar no painel.
2. Formato exato do manifesto de `x-signature` para `order` — validar com teste
   usando o exemplo oficial (`ts=1742505638683,v1=ced36a…`).
3. Credencial: o `AccessToken` commitado em `appsettings.Development.json` deve ser
   revogado e movido para segredo (fora do escopo desta feature, mas recomendado
   antes do primeiro teste real).
4. Pagador sem e-mail: o fallback fixo (`contato@gatumsgourmet.com.br`) da V1 não
   deve ser reproduzido às cegas; a V2 usa o e-mail do cliente do pedido/usuário e,
   se não houver, falha com mensagem clara. (Confirmar de onde sai o e-mail no plano.)
5. Swagger/OpenAPI é um documento único; rotas V2 aparecem na mesma UI com a tag
   `Pagamentos V2`.

## 7. Estratégia de testes (TDD)

- **Unit:** `IniciarPixV2Handler`, `ConsultarStatusV2Handler`,
  `ProcessarWebhookOrdersHandler`, `AplicarResultadoPagamentoPixService`,
  validador de assinatura (xUnit + Moq + FluentAssertions).
- **Provider:** `MercadoPagoOrdersPixProvider` com `HttpMessageHandler` falso,
  usando os JSONs de request/response de exemplo da documentação (inclui erro 4xx
  com `errors[]`).
- **Integração:** `WebApplicationFactory` para as rotas V2 (auth, 201, webhook 401/200).
- **Regressão:** suíte V1 existente deve passar sem alteração.

## 8. Rollout

1. Implementar V2 com `Mock` por padrão; testar com Access Token **de teste** e
   `payer.first_name = "APRO"`.
2. Configurar webhook de Order no painel (teste) e validar ponta a ponta.
3. Front migra para `/v2/pagamentos` quando decidido (mesmo corpo/resposta).
4. V1 permanece até a migração completa; remoção é decisão futura.
