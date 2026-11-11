# Pagamentos V2 (Pix via Mercado Pago Orders API) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expor `/api/v2/pagamentos` com Pix gerado pela Orders API do Mercado Pago (criação, status e webhook), sem alterar nada da V1.

**Architecture:** Novo módulo V2 em paralelo à V1: interface `IPixOrdersProvider` (Domain) implementada por `MercadoPagoOrdersPixProvider` (Infrastructure); handlers explícitos em `Application/UseCases/V2/Pagamentos`; endpoints em `Api/Endpoints/V2`. Reaproveita `TransacaoPagamento` (sem migration), marcando `Operadora = "mercadopago-orders"` e `OperadoraRef = ORD...`.

**Tech Stack:** .NET 10, Minimal APIs, EF Core/Npgsql, FluentValidation, xUnit + Moq + FluentAssertions, WebApplicationFactory.

**Spec:** `docs/superpowers/specs/2026-11-11-pagamentos-v2-orders-pix-design.md`

## Global Constraints

- **NUNCA commitar o Access Token do Mercado Pago** (nem WebhookSecret, nem qualquer credencial). `src/GatumsGourmet.Api/appsettings.Development.json` e `appsettings.LocalDocker.json` já estão versionados **com o token** — não modificar nem dar `git add` neles. Segredos novos só por variável de ambiente (`MercadoPago__WebhookSecret`, `MercadoPago__AccessToken`) ou `dotnet user-secrets`.
- Todo commit: `git add` apenas dos arquivos nomeados na tarefa (nunca `git add .`/`-A`) e, antes de `git commit`, rodar `powershell -File .superpowers/sdd/2026-11-11-pagamentos-v2-orders-pix/check-secrets.ps1` — deve terminar com "OK" (exit 0). O script falha se o diff staged contiver um *valor* que pareça credencial (`APP_USR-<dígitos>-…`, `TEST-…`, `WebhookSecret` com valor) ou qualquer `appsettings*.json`; citar o *nome* de uma chave de configuração em docs é permitido. Commits autorizados pelo usuário nesta execução.
- V1 intocada: nenhuma rota, handler, provider ou registro de DI existente muda de comportamento; suíte V1 passa sem alteração.
- Uma classe por arquivo; use case = Handler + Interface + Request/Response com `Result<T>`; arquivos `.cs` UTF-8 sem BOM, preservando acentuação.
- Valores monetários para o Mercado Pago como string `"50.00"` formatada com `CultureInfo.InvariantCulture`.
- Body da order: `type:"online"`, `processing_mode:"automatic"`, `external_reference` = `transacao.Id`, `payment_method:{id:"pix",type:"bank_transfer"}`, `payer.email` obrigatório, `config.payment.expiration_time` ISO 8601 (padrão `PT30M`, limites 30 min–30 dias); **não** enviar `expiration_time` no nível da order. Header `X-Idempotency-Key` = `transacao.Id`.
- Mapeamento: `processed/accredited` → `Aprovado`; `canceled` (qualquer detail) e `failed` → `Recusado`; demais → permanece `Pendente`.
- `OperadoraRef` = `ORD...`; `Operadora` = `"mercadopago-orders"`.
- Webhook: responde 200 para eventos válidos (inclusive transação desconhecida); 401 para assinatura inválida; não confia no body — sempre confirma via `GET /v1/orders/{id}`.

## Review Focus

1. Cultura pt-BR no servidor: `50.5m` deve serializar `"50.50"` (ponto), nunca `"50,50"` → teste na Task 1.
2. Usuário/pedido sem e-mail utilizável: falha clara, nada persistido, provider não chamado → Task 4.
3. Webhook reenviado para transação já `Aprovado`: idempotente, sem nova entrada na timeline → Task 3 e 6.
4. Assinatura ausente/inválida/`ts` fora da tolerância: 401; `type != order` ou sem `data.id`: 200 e ignorado → Task 2 e 7.
5. Resposta do Mercado Pago sem `qr_code`: falha, sem persistir Pix vazio → Task 1 e 4.

---

### Task 1: Contratos de domínio + provider Orders (e mock)

**Files:**
- Create: `src/GatumsGourmet.Domain/Services/Interfaces/IPixOrdersProvider.cs`
- Create: `src/GatumsGourmet.Domain/Entities/Pagamentos/PixOrderRequest.cs`, `PixOrderResult.cs`, `PixOrderStatus.cs`
- Create: `src/GatumsGourmet.Domain/Exceptions/PixOrdersProviderException.cs` (seguir a pasta de exceções existente do Domain)
- Create: `src/GatumsGourmet.Infrastructure/Services/Pagamentos/MercadoPagoOrdersPixProvider.cs`, `MockPixOrdersProvider.cs`
- Modify: `src/GatumsGourmet.Infrastructure/InfrastructureServiceCollectionExtensions.cs` (registro adicional, logo após o bloco do `IPixProvider`; não alterar esse bloco)
- Test: `tests/GatumsGourmet.UnitTests/Infrastructure/Services/Pagamentos/MercadoPagoOrdersPixProviderTests.cs` (confirmar que `UnitTests` referencia `Infrastructure`; se não, adicionar `ProjectReference`)

**Interfaces:**
- Produces:
  - `record PixOrderRequest(Guid TransacaoId, decimal Valor, string? Descricao, string PayerEmail, string? PayerNome)`
  - `record PixOrderResult(string OrderId, string CopiaCola, string? QrCodeBase64, string? TicketUrl, DateTime ExpiraEm)`
  - `record PixOrderStatus(string Status, string? StatusDetail)`
  - `sealed class PixOrdersProviderException(string message, int? statusCode = null) : Exception` (propriedade `StatusCode`)
  - `interface IPixOrdersProvider { Task<PixOrderResult> CriarOrderAsync(PixOrderRequest request, CancellationToken ct = default); Task<PixOrderStatus?> ConsultarOrderAsync(string orderId, CancellationToken ct = default); }`
  - `MercadoPagoOrdersPixProvider(HttpClient http, IConfiguration configuration, IClock clock, ILogger<MercadoPagoOrdersPixProvider> logger) : IPixOrdersProvider`

- [ ] **Step 1: Escrever os testes que falham** (`HttpMessageHandler` falso que captura request e devolve resposta):
  - `CriarOrderAsync_EnviaBodyEHeadersDoContrato`: asserta `POST v1/orders`, `Authorization: Bearer <token de teste>`, `X-Idempotency-Key == TransacaoId`, e no JSON `type=online`, `processing_mode=automatic`, `external_reference=<TransacaoId>`, `total_amount="50.00"`, `transactions.payments[0].amount="50.00"`, `payment_method.id=pix`, `payment_method.type=bank_transfer`, `payer.email`, `config.payment.expiration_time="PT30M"`, e ausência de `expiration_time` na raiz.
  - `CriarOrderAsync_ValorComCulturaPtBr_UsaPonto`: com `CultureInfo.CurrentCulture = pt-BR`, `50.5m` → `"50.50"`.
  - `CriarOrderAsync_RespostaDeSucesso_MapeiaOrderIdQrCodeETicket`: usa o JSON de exemplo da documentação (`ORD01HRYFWNYRE1MR1E60MW3X0T2P`, `qr_code`, `qr_code_base64`, `ticket_url`); `ExpiraEm == clock.UtcNow + 30 min`.
  - `CriarOrderAsync_RespostaSemQrCode_LancaPixOrdersProviderException`.
  - `CriarOrderAsync_Http400_LancaExcecaoComStatusEMensagemDoErrors` (corpo `{"errors":[{"code":"...","message":"..."}]}`; a mensagem da exceção contém `code` e `message`).
  - `ConsultarOrderAsync_Sucesso_RetornaStatusEStatusDetail` e `ConsultarOrderAsync_404_RetornaNull`.
  - `CriarOrderAsync_SemAccessToken_LancaInvalidOperationException`.

- [ ] **Step 2: Rodar e ver falhar**
  Run: `dotnet test tests/GatumsGourmet.UnitTests --filter FullyQualifiedName~MercadoPagoOrdersPixProviderTests`
  Expected: erro de compilação/FAIL (tipos inexistentes).

- [ ] **Step 3: Implementar** os tipos de domínio e o `MercadoPagoOrdersPixProvider` (DTOs privados com `JsonPropertyName`, mesmo estilo do `MercadoPagoPixProvider`; `MercadoPago:PixExpirationMinutes` com padrão 30 e *clamp* 30..43200 → ISO via `System.Xml.XmlConvert.ToString(TimeSpan)`/formato `PT{n}M`). Implementar `MockPixOrdersProvider` (devolve `ORD-MOCK-{guid}`, QR fake, status consultável como `processed/accredited`). Registrar: com `AccessToken` → `AddHttpClient<IPixOrdersProvider, MercadoPagoOrdersPixProvider>` (mesma `BaseUrl`, timeout 30 s); senão `AddScoped<IPixOrdersProvider, MockPixOrdersProvider>`.

- [ ] **Step 4: Rodar e ver passar**
  Run: mesmo comando do Step 2. Expected: PASS.

- [ ] **Step 5: Commit** (ver Global Constraints; `git add` só dos arquivos acima) — `feat(pagamentos-v2): provider Mercado Pago Orders API para Pix`

---

### Task 2: Validador de assinatura do webhook

**Files:**
- Create: `src/GatumsGourmet.Domain/Services/Interfaces/IMercadoPagoWebhookSignatureValidator.cs`
- Create: `src/GatumsGourmet.Infrastructure/Services/Pagamentos/MercadoPagoWebhookSignatureValidator.cs`
- Modify: `InfrastructureServiceCollectionExtensions.cs` (registro `AddSingleton`)
- Test: `tests/GatumsGourmet.UnitTests/Infrastructure/Services/Pagamentos/MercadoPagoWebhookSignatureValidatorTests.cs`

**Interfaces:**
- Produces: `interface IMercadoPagoWebhookSignatureValidator { bool IsValid(string? signatureHeader, string? requestId, string? dataId); }`; impl `MercadoPagoWebhookSignatureValidator(IConfiguration configuration, IHostEnvironment environment, IClock clock, ILogger<...> logger)`.

- [ ] **Step 1: Testes que falham:** `AssinaturaValida_RetornaTrue` (gera `v1` = HMAC-SHA256 hex minúsculo de `id:{dataId em minúsculas};request-id:{requestId};ts:{ts};` com segredo de teste e `ts` = agora); `AssinaturaAdulterada_RetornaFalse`; `HeaderAusente_RetornaFalse`; `TsForaDaTolerancia_RetornaFalse` (tolerância de 5 min); `SemSegredoEmDevelopment_RetornaTrue`; `SemSegredoForaDeDevelopment_RetornaFalse`. Acrescentar um teste de compatibilidade com o formato do exemplo oficial (`ts=1742505638683,v1=ced36a…`) apenas para o *parse* do header (`ts`/`v1`).
- [ ] **Step 2: Rodar e ver falhar:** `dotnet test tests/GatumsGourmet.UnitTests --filter FullyQualifiedName~MercadoPagoWebhookSignatureValidatorTests` → FAIL.
- [ ] **Step 3: Implementar** com `CryptographicOperations.FixedTimeEquals`; segredo em `MercadoPago:WebhookSecret`; `ts` em milissegundos Unix comparado com `clock.UtcNow`.
- [ ] **Step 4: Rodar e ver passar** → PASS.
- [ ] **Step 5: Commit** — `feat(pagamentos-v2): valida x-signature do webhook de orders`

---

### Task 3: Mapeamento de status e aplicação do resultado

**Files:**
- Create: `src/GatumsGourmet.Application/UseCases/V2/Pagamentos/PixOrderStatusMapper.cs`
- Create: `src/GatumsGourmet.Application/UseCases/V2/Pagamentos/AplicarResultadoPagamentoPixService.cs` (inclui `IAplicarResultadoPagamentoPixService`)
- Modify: `src/GatumsGourmet.Application/ApplicationServiceCollectionExtensions.cs` (registro `AddScoped`)
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V2/Pagamentos/AplicarResultadoPagamentoPixServiceTests.cs`

**Interfaces:**
- Produces:
  - `static class PixOrderStatusMapper { public static StatusTransacao? ToStatusTransacao(PixOrderStatus s); }` (`null` = continua pendente)
  - `interface IAplicarResultadoPagamentoPixService { Task<bool> AplicarAsync(TransacaoPagamento transacao, PixOrderStatus status, string origem, CancellationToken ct = default); }` — retorna `true` se mudou algo e salvou.
  - Implementação: `AplicarResultadoPagamentoPixService(ITransacaoRepository, IPedidoRepository, IClock, ILogger<...>)`.

- [ ] **Step 1: Testes que falham:** `Mapper` com tabela (`processed/accredited`→Aprovado; `canceled/expired`→Recusado; `canceled/canceled`→Recusado; `failed/*`→Recusado; `action_required/waiting_transfer`→null; `processing/in_process`→null). Serviço: `Aprovado_AtualizaTransacaoEPedido` (pedido em `Criado` → `Pagamento`, cria `PedidoPagamento{Resumo="Pix (Mercado Pago)"}` e 1 evento de timeline com a `origem` na observação, salva ambos); `Recusado_MarcaTransacaoRecusada_SemTocarNoPedido`; `TransacaoJaAprovada_NaoAlteraNada_RetornaFalse` (sem nova timeline, `SaveChangesAsync` nunca chamado); `StatusPendente_NaoAltera`.
- [ ] **Step 2: Rodar e ver falhar:** `dotnet test tests/GatumsGourmet.UnitTests --filter FullyQualifiedName~V2.Pagamentos.AplicarResultado` → FAIL.
- [ ] **Step 3: Implementar** copiando a *regra* (não o código) de `ProcessarWebhookMercadoPagoHandler`: só altera transação `Pendente`; pedido só avança se `StatusAtual is Criado or Aprovado`.
- [ ] **Step 4: Rodar e ver passar** → PASS.
- [ ] **Step 5: Commit** — `feat(pagamentos-v2): serviço de aplicação do resultado do pagamento Pix`

---

### Task 4: `IniciarPixV2Handler`

**Files:**
- Create: `src/GatumsGourmet.Application/UseCases/V2/Pagamentos/IniciarPix/IniciarPixV2Handler.cs` (contém `IIniciarPixV2Handler`; reutiliza `IniciarPixRequest`/`IniciarPixValidator` da V1)
- Modify: `ApplicationServiceCollectionExtensions.cs`
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V2/Pagamentos/IniciarPix/IniciarPixV2HandlerTests.cs`

**Interfaces:**
- Consumes: `IPixOrdersProvider`, `ITransacaoRepository`, `IPedidoRepository`, `IUsuarioRepository`, `IClock`, `TransacaoMapper`/`TransacaoResponse` (V1, inalterados).
- Produces: `interface IIniciarPixV2Handler { Task<Result<TransacaoResponse>> Handle(IniciarPixRequest request, string? idempotencyKey, CancellationToken ct = default); }`; construtor `IniciarPixV2Handler(ITransacaoRepository, IPedidoRepository, IUsuarioRepository, IPixOrdersProvider, IClock, ILogger<IniciarPixV2Handler>)`.

- [ ] **Step 1: Testes que falham** (mesmo estilo de `IniciarPixHandlerTests`):
  - `PedidoInexistente_Fail("Pedido não encontrado.")`.
  - `CriaOrder_PersisteQrExpiracaoOperadoraRefEOperadora`: `Operadora=="mercadopago-orders"`, `OperadoraRef=="ORD..."`, `Status==Pendente`, retorna `TransacaoResponse` com `PixCopiaCola`/`PixQrCodeBase64`; provider chamado com `PayerEmail` do `request` quando informado.
  - `SemEmailNoRequest_UsaEmailDoUsuarioDoPedido`.
  - `SemEmailEmLugarNenhum_Fail_NaoChamaProvider_NaoPersiste` (Review Focus 2).
  - `TransacaoPendenteV2Existente_Reaproveita_NaoChamaProvider`.
  - `TransacaoPendenteV1_NaoReaproveita_CriaNovaOrder` (existente com `Operadora="mercadopago"`/`OperadoraRef` numérico).
  - `ProviderLancaPixOrdersProviderException_Fail_ComMensagem_NaoPersiste` (inclui resposta sem QR — Review Focus 5).
- [ ] **Step 2: Rodar e ver falhar:** `dotnet test tests/GatumsGourmet.UnitTests --filter FullyQualifiedName~IniciarPixV2` → FAIL.
- [ ] **Step 3: Implementar** conforme spec §5.5; valor = `request.Valor ?? pedido.Total`; só `catch (PixOrdersProviderException)` → `Result.Fail(ex.Message)`.
- [ ] **Step 4: Rodar e ver passar** → PASS.
- [ ] **Step 5: Commit** — `feat(pagamentos-v2): iniciar Pix via Orders API`

---

### Task 5: `ConsultarStatusV2Handler`

**Files:**
- Create: `src/GatumsGourmet.Application/UseCases/V2/Pagamentos/ConsultarStatus/ConsultarStatusV2Handler.cs`
- Modify: `ApplicationServiceCollectionExtensions.cs`
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V2/Pagamentos/ConsultarStatus/ConsultarStatusV2HandlerTests.cs`

**Interfaces:**
- Consumes: `IAplicarResultadoPagamentoPixService` (Task 3), `IPixOrdersProvider` (Task 1).
- Produces: `interface IConsultarStatusV2Handler { Task<Result<TransacaoResponse>> Handle(Guid transacaoId, CancellationToken ct = default); }`; construtor `(ITransacaoRepository, IPixOrdersProvider, IAplicarResultadoPagamentoPixService, ILogger<...>)`.

- [ ] **Step 1: Testes que falham:** `TransacaoInexistente_Fail("Transação não encontrada.")`; `PendenteDaV2_ConsultaOrderEAplicaResultado`; `JaAprovada_NaoConsultaProvider`; `TransacaoDaV1_NaoConsultaProvider_RetornaComoEsta`; `ProviderRetornaNull_MantemPendente`; `ProviderLanca_MantemPendente_NaoQuebra` (consulta é *best effort*).
- [ ] **Step 2: Rodar e ver falhar:** `--filter FullyQualifiedName~ConsultarStatusV2` → FAIL.
- [ ] **Step 3: Implementar** (consulta só se `Pendente`, `Metodo==Pix`, `Operadora=="mercadopago-orders"` e `OperadoraRef` preenchida).
- [ ] **Step 4: Rodar e ver passar** → PASS.
- [ ] **Step 5: Commit** — `feat(pagamentos-v2): consulta de status de Pix via Orders API`

---

### Task 6: `ProcessarWebhookOrdersHandler`

**Files:**
- Create: `src/GatumsGourmet.Application/UseCases/V2/Pagamentos/ProcessarWebhookOrders/ProcessarWebhookOrdersHandler.cs`
- Modify: `ApplicationServiceCollectionExtensions.cs`
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V2/Pagamentos/ProcessarWebhookOrders/ProcessarWebhookOrdersHandlerTests.cs`

**Interfaces:**
- Produces: `interface IProcessarWebhookOrdersHandler { Task<Result<bool>> Handle(string? dataId, string? type, CancellationToken ct = default); }`; construtor `(ITransacaoRepository, IPixOrdersProvider, IAplicarResultadoPagamentoPixService, ILogger<...>)`.

- [ ] **Step 1: Testes que falham:** `SemDataId_Ok_Ignora`; `TypeDiferenteDeOrder_Ok_Ignora_NaoConsultaProvider`; `OrderPaga_AplicaResultado`; `OrderDesconhecida_Ok_NaoQuebra` (`GetByOperadoraRefAsync` → null); `Reenvio_TransacaoJaAprovada_Idempotente` (serviço devolve `false`, resposta ainda `Ok(true)`); `ProviderRetornaNull_Ok_SemAlterar`.
- [ ] **Step 2: Rodar e ver falhar:** `--filter FullyQualifiedName~ProcessarWebhookOrders` → FAIL.
- [ ] **Step 3: Implementar** conforme spec §5.6 (passos 2–5; a assinatura é validada no endpoint).
- [ ] **Step 4: Rodar e ver passar** → PASS.
- [ ] **Step 5: Commit** — `feat(pagamentos-v2): processar webhook de orders do Mercado Pago`

---

### Task 7: Endpoints V2 e wiring

**Files:**
- Create: `src/GatumsGourmet.Api/Endpoints/V2/PagamentosEndpoints.cs` (namespace `GatumsGourmet.Api.Endpoints.V2`; `public static void Map(RouteGroupBuilder group)`)
- Modify: `src/GatumsGourmet.Api/Extensions/EndpointRouteBuilderExtensions.cs` (adicionar `MapV2Endpoints()` privado, chamado em `MapGatumsEndpoints`, tag `Pagamentos V2`; atualizar o comentário)
- Test: `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V2/Pagamentos/PagamentosV2EndpointTests.cs` (usa `CustomWebApplicationFactory`/`AuthenticatedClientFactory`; exige o Postgres de teste já rodando)

**Interfaces:**
- Consumes: `IIniciarPixV2Handler`, `IConsultarStatusV2Handler`, `IProcessarWebhookOrdersHandler`, `IMercadoPagoWebhookSignatureValidator`, e os handlers V1 `IListOpcoesPagamentoHandler`, `IProcessarCartaoCreditoHandler`, `IProcessarCartaoDebitoHandler`.
- Rotas (spec §5.1): `POST /pix` (201 via `ToHttpResult(201)`, `Idempotency-Key` opcional, `RequireAuthorization`), `GET /pix/{transacaoId:guid}/status`, `GET /{transacaoId:guid}/status`, `POST /webhook/mercadopago` (`AllowAnonymous`; lê `data.id`/`type` da query com fallback para `MercadoPagoWebhookBody` da V1; headers `x-signature`, `x-request-id`; assinatura inválida → `Results.Unauthorized()`), `GET /opcoes`, `POST /cartao-credito`, `POST /cartao-debito` (cópias finas).

- [ ] **Step 1: Testes de integração que falham:** `PostPixV2_SemToken_401`; `PostPixV2_ComPedidoValido_201_ComQrEOperadoraMercadoPagoOrders` (usa o Mock — ambiente `Test` sem AccessToken; cria pedido pelo fluxo de checkout já existente nos testes V1); `GetStatusV2_Retorna200`; `Webhook_SemAssinatura_401` (Test ≠ Development e sem segredo); `GetOpcoesV2_Retorna200`; `RotasV1_ContinuamResponding` (smoke: `GET /api/v1/pagamentos/opcoes` autenticado → 200).
- [ ] **Step 2: Rodar e ver falhar:** `dotnet test tests/GatumsGourmet.IntegrationTests --filter FullyQualifiedName~PagamentosV2` → FAIL (404).
- [ ] **Step 3: Implementar** o mapeamento e o `MapV2Endpoints`.
- [ ] **Step 4: Rodar e ver passar** o filtro acima → PASS.
- [ ] **Step 5: Commit** — `feat(pagamentos-v2): endpoints /api/v2/pagamentos`

---

### Task 8: Documentação, configuração e verificação final

**Files:**
- Modify: `docs/Integracao-MercadoPago-Pix.md` (seção V2: contrato Orders, teste com `first_name="APRO"`, painel: ativar evento **Order**, `MercadoPago:WebhookSecret`/`PixExpirationMinutes` — **sem valores reais**), `docs/CHANGELOG.md`, `docs/Site/API-Reference.md` (rotas V2), `README.md` (nota de evolução curta), `docs/superpowers/README.md` (linha da feature na tabela)
- Create: `docs/superpowers/execucao/pagamentos-v2-orders/progress.md` (somente se a execução for via subagent-driven)
- **Não tocar:** `appsettings.Development.json`, `appsettings.LocalDocker.json`, `appsettings.json` com credenciais.

- [ ] **Step 1:** Atualizar os docs acima; chaves de config citadas com placeholder (`<definir via user-secrets>`).
- [ ] **Step 2: Build limpo:** `dotnet build GatumsGourmet.sln` (ou o `.sln` da raiz) → 0 erros.
- [ ] **Step 3: Suíte unitária completa:** `dotnet test tests/GatumsGourmet.UnitTests` → tudo verde (inclui V1 inalterada).
- [ ] **Step 4: Suíte de integração:** `dotnet test tests/GatumsGourmet.IntegrationTests` (Postgres de teste ativo) → tudo verde.
- [ ] **Step 5: Varredura de segredos:** `git status` sem `appsettings*.json` modificado; `git diff develop...HEAD | Select-String '^\+.*(APP_USR-\d+-\d+-[0-9a-fA-F]{16,})'` não retorna nada.
- [ ] **Step 6: Teste manual (fora do CI, só com o usuário):** com Access Token **de teste** em `dotnet user-secrets`, `POST /api/v2/pagamentos/pix` com `payerNome="APRO"` → 201 com QR; consultar status até `aprovado`.
- [ ] **Step 7: Commit** — `docs(pagamentos-v2): documentação da V2 de pagamentos Pix`
