# Changelog — Gatums Gourmet API

Histórico de mudanças da API Gatums Gourmet para referência do site (front-end).

## 2026-11-10 — Pagamentos: Mercado Pago Pix + Separação por Métodos (Pix, Débito, Crédito)

### Novos endpoints de Pagamento
| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| POST | `/api/v1/pagamentos/pix` | Autenticado | Iniciar pagamento via Pix (Mercado Pago). Cria ou reaproveita transação pendente |
| GET | `/api/v1/pagamentos/pix/{transacaoId}/status` | Autenticado | Consulta status do Pix em tempo real no gateway e sincroniza automaticamente o pedido |
| POST | `/api/v1/pagamentos/webhook/mercadopago` | **Público** | Webhook para notificações do Mercado Pago (consulta a API oficial antes de aprovar) |
| POST | `/api/v1/pagamentos/cartao-credito` | Autenticado | Processar pagamento com cartão de crédito (com suporte a parcelamento) |
| POST | `/api/v1/pagamentos/cartao-debito` | Autenticado | Processar pagamento com cartão de débito |

### Integração Mercado Pago
- **Provedor Real (`MercadoPagoPixProvider`):** Comunicação direta via HTTP com a API REST v1 do Mercado Pago (`POST /v1/payments`).
- **Injeção Condicional:** Se `MercadoPago:AccessToken` estiver configurado, utiliza o provedor real do Mercado Pago; caso contrário, utiliza o `MockPixProvider` para desenvolvimento/testes locais.
- **QR Code Base64:** O Pix agora retorna tanto a chave `pixCopiaCola` quanto a imagem `pixQrCodeBase64` pronta para renderização direta no front-end.
- **Idempotência:** Header `X-Idempotency-Key` enviado em todas as chamadas para prevenir duplicidade de cobranças.
- **Sincronização em Tempo Real (Polling / Webhook):** Ao consultar o status em `GET /api/v1/pagamentos/pix/{id}/status`, a API consulta o gateway do Mercado Pago imediatamente. Se já estiver pago, aprova a transação e o pedido no banco local sem depender exclusivamente do webhook.
- **Webhook Seguro:** O endpoint `/api/v1/pagamentos/webhook/mercadopago` é público, mas busca os detalhes da transação diretamente na API oficial do Mercado Pago usando o token secreto da aplicação antes de aprovar qualquer pedido.

### Nova Documentação
- **`docs/CONTEXT.md`:** Documento mestre de contexto da API (.NET 10, Clean Architecture, CQRS, Minimal APIs, RBAC, workflows e diretrizes para IAs/LLMs).
- **`docs/Integracao-MercadoPago-Pix.md`:** Guia passo a passo completo para criação da conta no Mercado Pago Developers, obtenção de credenciais de teste e produção, configuração de Webhook e URLs.

### Mudanças no front-end recomendadas
- **Fluxo Pix Dedicado:** Migrar o front-end para usar `POST /api/v1/pagamentos/pix` com `{ "pedidoId": "guid", "valor": 99.90 }` (valor é opcional; se omitido, assume o total do pedido).
- **Renderização do QR Code:** Exibir imagem diretamente com `<img src="data:image/png;base64,{pixQrCodeBase64}" />` e botão "Copiar código" com `pixCopiaCola`.
- **Status em Tempo Real:** O front-end pode fazer polling a cada 3 a 5 segundos em `GET /api/v1/pagamentos/pix/{transacaoId}/status`. Assim que o status for `"aprovado"`, redirecionar para a tela de confirmação do pedido.
- **Cartões Dedicados:** Usar `POST /api/v1/pagamentos/cartao-credito` (aceita `parcelas`) e `POST /api/v1/pagamentos/cartao-debito`.
- **Compatibilidade:** As rotas antigas (`/iniciar`, `/{id}/cartao`, `/{id}/status`, `/{id}/pix/confirmar` e `/{id}/validar`) continuam totalmente funcionais.

---

## 2026-11-08 — Controle de Estoque

### Novos endpoints
| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| GET | `/api/v1/produtos/{id}/estoque` | `produtos:ler` | Ver estoque atual + mínimo |
| PUT | `/api/v1/produtos/{id}/estoque` | `estoque:gerenciar` | Ajustar estoque (Entrada/Saída/Ajuste) |
| GET | `/api/v1/produtos/{id}/estoque/movimentos` | `produtos:ler` | Histórico de movimentos |
| GET | `/api/v1/estoque/baixo` | `estoque:gerenciar` | Produtos com estoque ≤ mínimo |

### Mudanças no front-end necessárias
- **Criar Produto** (`POST /produtos`): body agora aceita `estoqueInicial` (default 0) e `estoqueMinimo` (default 0)
- **Atualizar Produto** (`PUT /produtos/{id}`): body agora aceita `estoqueMinimo` (default 0)
- **Listar Produtos** (`GET /produtos`): response agora inclui `estoque` e `estoqueMinimo` em cada item
- **Obter Produto** (`GET /produtos/{id}`): response agora inclui `estoque` e `estoqueMinimo`
- **Adicionar ao Carrinho** (`POST /carrinho/itens`): pode retornar 400 "Estoque insuficiente" se a quantidade exceder o estoque
- **Checkout** (`POST /pedidos/checkout`): pode retornar 400 "Estoque insuficiente para 'X'. Disponível: Y, solicitado: Z." se algum produto não tiver estoque suficiente

### Nova permissão
- `estoque:gerenciar` — vinculada a Admin e Gerente (seed automático na migration)

### Nova entidade: MovimentoEstoque
Tabela auxiliar de auditoria. Cada ajuste de estoque gera um registro:
```json
{
  "id": "guid",
  "produtoId": "guid",
  "tipo": "entrada|saida|ajuste|venda",
  "quantidade": 10,        // positivo para entrada, negativo para saída/venda
  "saldoResultante": 45,    // saldo após o movimento
  "pedidoId": "guid|null",  // apenas quando tipo=venda
  "usuarioId": "guid|null", // quem ajustou
  "observacao": "string|null",
  "data": "2026-11-08T12:00:00Z"
}
```

### Novo enum: TipoMovimentoEstoque
| Valor | Nome | Descrição |
|-------|------|-----------|
| 0 | Entrada | Reposição de estoque |
| 1 | Saida | Saída manual |
| 2 | Ajuste | Correção de inventário (+/-) |
| 3 | Venda | Débito automático no checkout |

---

## 2026-11-07 — Logging + Docker Hub

### Serilog + Seq
- Adicionado Serilog como provider de logging (substitui o ILogger padrão sem mudar código existente)
- Seq adicionado ao `docker-compose.yml` local (porta 5341, http://localhost:5341)
- Logs estruturados com CorrelationId (filtra por request no Seq)
- Controle de verbosidade por appsettings: `Warning` em produção (só 4xx/5xx), `Debug` em desenvolvimento

### Docker Hub Publishing
- Criado GitHub Action `docker-publish.yml` que builda e publica a imagem da API no Docker Hub
- Tags: `latest`, `:main`, `:release`, `:<sha>`

### Validação de imagem 5MB
- Corrigido erro 500 quando imagem > 5MB: agora retorna 400 com mensagem "Arquivo excede o tamanho máximo de 5 MB."
- Limite do framework aumentado de 6MB para 50MB para dar espaço ao handler validar

---

## 2026-11-06 — Supabase Storage + Render HML

### Correções do Supabase S3 (Forbidden)
- `UseHttp` agora deriva do protocolo do `ServiceUrl` (era hardcoded `true`, quebrava HTTPS)
- Corrigido `PublicBaseUrl` de `/rest/v1/` para `/storage/v1/object/public/gatums-imagens`
- Corrigido `ServiceUrl` no `appsettings.Release.json` (apontava para URL do Render em vez do Supabase)
- Corrigido região de `us-east-1` para `us-west-2` (confirmado pelo dashboard do Supabase)
- Downgrade do AWSSDK.S3 de v4 (`4.0.104.7`) para v3 (`3.7.511.8`) — v4 quebra S3-compatíveis
- Adicionado fallback `ListObjectsV2` → `ListObjects v1` no `S3ImageStorage`
- Adicionado `ILogger<S3ImageStorage>` para diagnosticar erros S3
- Adicionado `OrphanCleanupEnabled` config flag

### Render HML
- Criado `render.yaml` (blueprint do Render)
- Atualizado `.env.example` com todas as variáveis necessárias
- Criado `docs/Deploy-Render-HML.md` (guia passo a passo)

### Orphan Cleanup
- `OrphanImageCleanupService` agora loga "Iniciando..." e resultado de falha não-exceção
- Deep exception logging em ambos background services

---

## 2026-11-05 — Deep Exception Logging + Request Logging

### Deep Exception Logger
- Criado `DeepExceptionLogger` — extensão que percorre `InnerException` recursivamente + `AggregateException` + `ReflectionTypeLoadException`
- `ExceptionMiddleware` melhorado: contexto da requisição (método, path, user, requestId), ProblemDetails com `traceId`, 500 não vaza `ex.Message`
- `OperationCanceledException` tratada (não vira 500)
- Background services (`OrphanImageCleanupService`, `RefreshTokenCleanupService`) agora usam `LogDeep()` e logam falhas não-exceção

### Request Logging Middleware
- Criado `RequestLoggingMiddleware` — loga cada request: `→ {Method} {Path} | RequestId | User` e `← {Method} {Path} → {Status} ({ElapsedMs}ms)`

---

## 2026-11-04 — Postman + Site Docs + CI/CD + Docker

### Postman
- Criada coleção Postman completa com todos os endpoints, variáveis encadeadas e scripts de teste
- Criado ambiente local com credenciais seed
- Atualizado com endpoints de estoque (5 novos requests)

### Documentação do Site
- Criado `docs/Site/` com 6 arquivos de referência para integração do front:
  - `README.md` — visão geral
  - `Autenticacao.md` — JWT, login, registro, roles
  - `API-Reference.md` — todos os endpoints
  - `Fluxos-Integracao.md` — fluxos completos (cliente, admin, pagamento)
  - `Modelos-de-Dados.md` — todos os DTOs
  - `Erros-e-Padroes.md` — ProblemDetails, enums, convenções

### CI/CD Pipeline
- Criado `deploy.yml` — build + test + deploy automático para VPS Locaweb
- Criado `release-hml.yml` — build + test + deploy hook para Render
- Criado `docker-publish.yml` — build + push para Docker Hub

### Docker
- Atualizado `docker/Dockerfile` de .NET 8 para .NET 10 (todos projetos usam `net10.0`)
- Criado `docker-compose.production.yml` (API + PostgreSQL para VPS)
- Criado `.dockerignore`
- Criado `deploy/deploy-locaweb.sh` e `deploy/setup-vps.sh`
- Seq adicionado ao `docker/docker-compose.yml` (porta 5341)

### Deploy Docs
- Criado `docs/Deploy-Locaweb.md` — guia passo a passo Locaweb
- Criado `docs/Deploy-Render-HML.md` — guia passo a passo Render HML

---

## Avisos importantes para o front-end

1. **Enums no body JSON são inteiros** (sem `JsonStringEnumConverter`): `tipo: 0` (Simples), `metodo: 2` (Pix), etc. Query params aceitam strings (`?tipo=simples`).
2. **`metodo` de pagamento tem 3 formatos**: `"cartao_credito"` em `/pagamentos/opcoes`, `1` (int) em `/pagamentos/iniciar`, `"cartaocredito"` na `TransacaoResponse`.
3. **Cliente ≠ Usuário**: o CRUD de `/clientes` é um CRM admin, distinto do `/auth/register`.
4. **Validadores não são invocados**: validação de campos deve ser feita no front.
5. **Estoque**: o checkout agora valida estoque. Se o front não tratar o 400 "Estoque insuficiente", o usuário verá um erro genérico. Adicionar tratamento específico.
6. **Novos campos em produtos**: `estoque` e `estoqueMinimo` estão nas responses de listar e obter produto. O front pode exibir badges de "Estoque baixo" quando `estoque <= estoqueMinimo`.
7. **QR Code Base64 no Pix**: `TransacaoResponse` agora inclui `pixQrCodeBase64`. O front pode renderizar a imagem diretamente sem precisar de gerador de QR code no client (`<img src="data:image/png;base64,${pixQrCodeBase64}" />`).
8. **Separação de Endpoints de Pagamento**: Prefira os novos endpoints dedicados `/pagamentos/pix`, `/pagamentos/cartao-credito` e `/pagamentos/cartao-debito`. As rotas legadas `/iniciar` e `/{id}/cartao` continuam disponíveis para retrocompatibilidade.
