# Contexto da API — Gatums Gourmet (.NET 10)

> **Documento Mestre de Contexto de Engenharia & Arquitetura**  
> *Destinado a desenvolvedores humanos, arquitetos de software e agentes de IA (LLMs) que colaboram no desenvolvimento, manutenção, extensão e integração da API Gatums Gourmet.*

---

## 1. Visão Geral do Sistema

O **Gatums Gourmet API** é o backend de e-commerce e delivery gastronômico da marca Gatums Gourmet. A API fornece serviços completos para gestão de catálogo de produtos, controle e movimentação de estoque, CRM de clientes, carrinho de compras com retenção de sessão, ciclo de vida do pedido (workflow de estados), faturamento e processamento de pagamentos múltiplos (Pix via Mercado Pago, Cartão de Crédito e Cartão de Débito), gestão de usuários com controle de acesso baseado em papéis e permissões (RBAC) e relatórios executivos de vendas.

### Princípios de Design & Arquitetura
1. **Clean Architecture / Arquitetura Hexagonal:** Desacoplamento estrito entre Domínio, Aplicação, Infraestrutura e Apresentação (API).
2. **Result Pattern:** Ausência de exceções para fluxo de controle de negócio. Toda operação da camada de aplicação retorna `Result` ou `Result<T>` com sucesso explícito ou falha tipada com código de erro e mensagem amigável.
3. **CQRS Leve (Use Cases):** Separação de comandos e consultas em handlers individuais (`Handler<Request, Result<Response>>`).
4. **Minimal APIs com Typed Results:** Rotas organizadas por agregados usando `RouteGroupBuilder` e retornos tipados (`Results<Ok<T>, BadRequest<ProblemDetails>, ...>`).
5. **Auditoria e Rastreabilidade:** Logs estruturados com Serilog, integração com Seq, cabeçalho de correlação `X-Correlation-Id` e rastreamento recursivo de exceções (`DeepExceptionLogger`).

---

## 2. Stack Tecnológica

| Camada / Função | Tecnologia | Versão / Detalhes |
|-----------------|------------|-------------------|
| **Linguagem & Runtime** | C# 13 / .NET | `net10.0` (LTS/Modern .NET) |
| **API Framework** | ASP.NET Core Minimal APIs | Endpoints declarativos com grupos versionados (`/api/v1`) |
| **ORM & Banco de Dados** | Entity Framework Core 10 / Npgsql | PostgreSQL (com migrações EF Core automáticas) |
| **Autenticação & Segurança** | JWT Bearer Tokens | Access token curto (~15m) + Refresh token rotativo (~7d) com revogação de família |
| **Autorização** | RBAC Customizado | Permissões granulares no formato `recurso:acao` validadas por policy |
| **Gateway de Pagamento** | Mercado Pago REST API v1 | Pix com QR Code dinâmico, Copia-e-Cola, Webhooks e consulta em tempo real |
| **Armazenamento de Imagens** | Supabase Storage (S3 API) | AWSSDK.S3 v3 com bucket público e rotina de limpeza de órfãos |
| **Logs & Observabilidade** | Serilog + Seq | Enriquecimento com CorrelationId, RequestLoggingMiddleware, ProblemDetails RFC 7807 |
| **Validação** | FluentValidation | Contratos de DTOs e entidades |
| **Testes** | xUnit, Moq, FluentAssertions | Testes de unidade para Use Cases e Domínio |

---

## 3. Estrutura do Repositório (Clean Architecture)

```
gatums-gourmet-api/
├── src/
│   ├── GatumsGourmet.Domain/              # Regras de Negócio Puras (Sem dependências externas)
│   │   ├── Common/                       # BaseEntity, ValueObject, Result pattern
│   │   ├── Entities/                     # Agregados (Produtos, Pedidos, Carrinho, Clientes, Pagamentos, etc.)
│   │   ├── Enums/                        # MetodoPagamento, PedidoEtapa, StatusTransacao, TipoMovimentoEstoque...
│   │   ├── Events/                       # Domain Events (PedidoCriadoEvent, PagamentoAprovadoEvent...)
│   │   ├── Repositories/                 # Interfaces de repositórios (IProdutoRepository, IPedidoRepository...)
│   │   └── Services/Interfaces/          # Interfaces de serviços externos (IPixProvider, IImageStorage...)
│   │
│   ├── GatumsGourmet.Application/         # Casos de Uso (Orquestração e Regras de Aplicação)
│   │   ├── Common/                       # DTOs compartilhados, PagedResult, BaseHandler
│   │   ├── Security/                     # Constantes de permissões e roles (Permissions.cs, Roles.cs)
│   │   └── UseCases/V1/                  # Casos de uso divididos por agregado:
│   │       ├── Auth/                     # Login, Register, Refresh, Logout, Me, PurgeTokens
│   │       ├── Categorias/               # Criar, Atualizar, Listar, Obter, Deletar
│   │       ├── Produtos/                 # Criar, Atualizar, Listar, Obter, Deletar, Estoque
│   │       ├── Clientes/                 # CRM de clientes (Criar, Atualizar, Listar, Deletar)
│   │       ├── Carrinho/                 # Obter, AdicionarItem, AtualizarItem, RemoverItem, Limpar
│   │       ├── Pedidos/                  # Checkout, Obter, Listar, AvancarEtapa, ConcluirPagamento
│   │       ├── Pagamentos/               # IniciarPix, ConsultarPix, Webhook, Credito, Debito, Opcoes
│   │       ├── Enderecos/                # Consulta de CEP (ViaCEP)
│   │       ├── Imagens/                  # Upload de arquivos (Multipart)
│   │       ├── Rbac/                     # Gestão de Roles, Permissions e Usuários
│   │       └── Relatorios/               # Resumo executivo de vendas
│   │
│   ├── GatumsGourmet.Infrastructure/     # Acesso a Dados e Integrações Externas
│   │   ├── Data/                         # GatumsDbContext, Entity Configurations, Repositórios concretos
│   │   ├── Migrations/                   # Histórico de migrações do PostgreSQL
│   │   ├── Services/                     # Implementações (MercadoPagoPixProvider, MockPixProvider, S3ImageStorage)
│   │   └── BackgroundServices/           # OrphanImageCleanupService, RefreshTokenCleanupService
│   │
│   └── GatumsGourmet.Api/                # Camada Web / Apresentação
│       ├── Endpoints/V1/                 # Definição das Minimal APIs por módulo
│       ├── Middleware/                   # ExceptionMiddleware, RequestLoggingMiddleware
│       ├── Program.cs                    # Configuração de DI, Middlewares e Pipelines
│       ├── appsettings.json              # Configurações gerais
│       └── appsettings.Development.json  # Configurações de dev local
│
├── tests/
│   ├── GatumsGourmet.UnitTests/          # Testes unitários (Use Cases, Domínio)
│   └── GatumsGourmet.IntegrationTests/   # Testes integrados (API, DB em memória/testcontainers)
│
├── docs/                                 # Documentação de Projeto e Guias
│   ├── CONTEXT.md                        # ESTE DOCUMENTO (Contexto mestre para humanos e IAs)
│   ├── CHANGELOG.md                      # Histórico cronológico de lançamentos e alterações da API
│   ├── Integracao-MercadoPago-Pix.md     # Guia passo a passo de setup e credenciais do Mercado Pago
│   ├── Deploy-Locaweb.md                 # Procedimentos de deploy VPS Locaweb
│   ├── Deploy-Render-HML.md              # Procedimentos de deploy no ambiente HML (Render)
│   ├── Postman/                          # Coleção E2E runner-ready e ambiente local
│   └── Site/                             # Documentação técnica para os desenvolvedores do front-end
└── docker/                               # Dockerfile e Docker Compose para dev e prod
```

---

## 4. Segurança, Autenticação e RBAC

### Mecanismo de Tokens JWT
- **Access Token:** Tempo de vida curto (padrão: 15 minutos). Armazenado no header `Authorization: Bearer <token>`. Contém claims de `sub` (ID do usuário), `email`, `nome` e lista de permissões granulares.
- **Refresh Token:** Tempo de vida longo (padrão: 7 dias), armazenado em banco com hashing. O sistema implementa **rotação de tokens**: a cada uso do refresh token, um novo par é gerado e o antigo é revogado.
- **Detecção de Reúso:** Se um refresh token já revogado for apresentado, o sistema detecta uma tentativa de invasão e invalida imediatamente toda a árvore/família de tokens daquele usuário, exigindo novo login.
- **Limpeza Periódica:** Um `BackgroundService` (`RefreshTokenCleanupService`) executa a cada 24 horas expurgando tokens expirados do banco de dados.

### Níveis de Papéis (Roles)
- **Admin:** Acesso irrestrito a todos os recursos do sistema.
- **Gerente:** Acesso a operações de catálogo, visualização de pedidos, relatórios e gestão de estoque.
- **Cliente:** Papel atribuído automaticamente a usuários cadastrados pelo aplicativo/web (`POST /auth/register`). Pode gerenciar o próprio carrinho, criar pedidos e efetuar pagamentos.

### Tabela de Permissões Granulares
| Permissão | Finalidade | Roles Padrão |
|-----------|------------|--------------|
| `produtos:ler` | Visualizar produtos e consultar estoque | Admin, Gerente, Cliente |
| `produtos:escrever` | Criar, alterar e remover produtos do catálogo | Admin, Gerente |
| `categorias:escrever` | Gerenciar categorias de produtos | Admin, Gerente |
| `estoque:gerenciar` | Entradas, saídas e ajustes manuais de estoque | Admin, Gerente |
| `clientes:ler` | Listar e consultar clientes do CRM | Admin, Gerente |
| `clientes:escrever` | Cadastrar, alterar e remover clientes no CRM | Admin, Gerente |
| `pedidos:ler` | Consultar todos os pedidos e detalhes da loja | Admin, Gerente |
| `pedidos:avancar` | Mudar a etapa do pedido no fluxo de atendimento | Admin, Gerente |
| `pagamentos:validar` | Validar manualmente transações pendentes | Admin |
| `permissoes:gerenciar` | Visualizar e editar a matriz de permissões | Admin |
| `roles:gerenciar` | Criar e modificar roles e vincular permissões | Admin |
| `usuarios:gerenciar` | Criar operadores, atribuir roles e ativar/desativar | Admin |
| `relatorios:ler` | Acessar dados analíticos e consolidados de vendas | Admin, Gerente |

---

## 5. Modelos de Domínio e Máquinas de Estado

### 5.1 Ciclo de Vida do Pedido (`PedidoEtapa`)
O workflow do pedido é estritamente controlado por uma máquina de estados:

```
[0: Criado] ──(Admin aprova)──> [1: Aprovado] ──(Cliente inicia pagto)──> [2: Pagamento]
                                                                                │
                                                                   (Pagamento confirmado)
                                                                                ▼
[6: Finalizado] <──(Admin)── [5: Entregue] <──(Admin)── [4: EnvioPendente] <── [3: PagamentoAprovado]
```

- **Criado (0):** Pedido gerado a partir do checkout do carrinho. Estoque reservado/debitado.
- **Aprovado (1):** Loja confirmou disponibilidade e aceitou o pedido.
- **Pagamento (2):** Cliente solicitou a cobrança (Pix gerado ou cartão submetido).
- **PagamentoAprovado (3):** Gateway confirmou o pagamento (via webhook, polling ou confirmação mock).
- **EnvioPendente (4):** Cozinha/balcão preparou e embalou o pedido para entrega ou retirada.
- **Entregue (5):** Pedido saiu e chegou ao cliente. A transição para esta etapa exige obrigatoriamente o envio de `EntregaTipo` (`0 = Transporte` ou `1 = Retirada`).
- **Finalizado (6):** Pedido concluído com sucesso e arquivado para métricas.
- **Cancelado (-1):** Pedido cancelado (estorno/reposição de estoque acionada conforme regra).

### 5.2 Transação de Pagamento (`StatusTransacao`)
- **Pendente (0):** Transação aberta aguardando liquidação.
- **Aprovado (1):** Valor recebido e validado. Gatilho automático para avançar o pedido para `PagamentoAprovado`.
- **Recusado (2):** Cartão negado ou falha operacional.
- **Expirado (3):** Tempo limite do Pix esgotado (padrão: 30 minutos).

### 5.3 Controle de Estoque & Auditoria (`MovimentoEstoque`)
- **Tipos de Movimento:**
  - `Entrada (0)`: Reposição de mercadorias.
  - `Saida (1)`: Perdas, avarias ou consumo interno.
  - `Ajuste (2)`: Correção de inventário físico.
  - `Venda (3)`: Débito automático executado no checkout do pedido.
- **Garantia de Integridade:** No checkout (`POST /pedidos/checkout`), o sistema valida os saldos de todos os itens do carrinho com concorrência otimista. Se qualquer item tiver saldo inferior ao solicitado, o pedido é rejeitado com status 400 (`Estoque insuficiente para 'X'. Disponível: Y, solicitado: Z`).
- **Alerta de Estoque Baixo:** Produtos com estoque atual menor ou igual ao `EstoqueMinimo` são listados no endpoint `/api/v1/estoque/baixo`.

---

## 6. Módulo de Pagamentos & Integração Mercado Pago

A API adota separação explícita dos canais de pagamento por método, oferecendo endpoints especializados e integração nativa com o **Mercado Pago** para Pix.

### 6.1 Arquitetura de Pagamentos
- **Interface Provedora (`IPixProvider`):** Abstrai o gateway de pagamento.
  - `MercadoPagoPixProvider`: Comunica-se diretamente via HTTP com o endpoint `https://api.mercadopago.com/v1/payments`.
  - `MockPixProvider`: Provedor de fallback para testes locais quando não há `AccessToken` configurado.
- **Injeção de Dependência Condicional:**
  Se `MercadoPago:AccessToken` estiver preenchido em `appsettings.json` ou variáveis de ambiente, o `MercadoPagoPixProvider` é injetado como Singleton com `HttpClient` tipado e timeout de 30 segundos; caso contrário, `MockPixProvider` assume sem interromper o ambiente de testes.

### 6.2 Endpoints Especializados de Pagamento
| Método | Rota | Autenticação | Descrição |
|--------|------|--------------|-----------|
| `POST` | `/api/v1/pagamentos/pix` | Bearer (Cliente/Admin) | Inicia cobrança Pix. Cria ou reutiliza transação pendente, invoca o Mercado Pago e devolve Copia-e-Cola e QR Code Base64. |
| `GET` | `/api/v1/pagamentos/pix/{transacaoId}/status` | Bearer (Cliente/Admin) | Consulta status em tempo real no Mercado Pago. Se aprovado lá, aprova automaticamente a transação e o pedido no banco local. |
| `POST` | `/api/v1/pagamentos/webhook/mercadopago` | **Público** (AllowAnonymous) | Recebe notificações de eventos IPN/Webhook do Mercado Pago (`payment.updated`). Consulta o status na API oficial e aprova o pedido. |
| `POST` | `/api/v1/pagamentos/cartao-credito` | Bearer (Cliente/Admin) | Processa pagamento por cartão de crédito (suporte a número, validade, CVV, titular e parcelas). |
| `POST` | `/api/v1/pagamentos/cartao-debito` | Bearer (Cliente/Admin) | Processa pagamento por cartão de débito. |
| `GET` | `/api/v1/pagamentos/opcoes` | **Público** | Lista as bandeiras e métodos de pagamento aceitos e número máximo de parcelas. |

### 6.3 Rotas Legadas (Compatibilidade)
Mantidas para assegurar compatibilidade contínua com clientes legados e testes pré-existentes:
- `POST /api/v1/pagamentos/iniciar`
- `POST /api/v1/pagamentos/{id}/cartao`
- `GET /api/v1/pagamentos/{id}/status`
- `POST /api/v1/pagamentos/{id}/pix/confirmar`
- `POST /api/v1/pagamentos/{id}/validar`

### 6.4 Tratamento de Idempotência e Segurança no Pix
- **Idempotency Key:** Toda requisição ao Mercado Pago envia o header `X-Idempotency-Key` baseado no identificador único da transação, garantindo que retentativas de rede nunca cobrem o cliente duas vezes.
- **Validação de Webhook:** O webhook público **não confia cega e diretamente no payload recebido**. Ele extrai o `data.id` da notificação e consulta o endpoint oficial autenticado `GET /v1/payments/{id}` usando o token secreto da aplicação antes de promover qualquer alteração no banco de dados.

---

## 7. Mapeamento Completo de Endpoints da API

### Autenticação & Conta (`/api/v1/auth`)
- `POST /auth/login` — Autenticação com e-mail e senha. Retorna JWT, Refresh Token e perfil.
- `POST /auth/register` — Cadastro de novo cliente.
- `POST /auth/refresh` — Rotação de refresh token.
- `POST /auth/logout` — Revogação do refresh token atual.
- `POST /auth/logout-all` — Revogação de todas as sessões ativas do usuário.
- `GET /auth/me` — Dados do usuário logado e suas permissões ativas.
- `POST /auth/maintenance/purge-tokens` — *[permissoes:gerenciar]* Expurgo manual de tokens expirados.

### Categorias (`/api/v1/categorias`)
- `GET /categorias` — *(Público)* Lista paginada de categorias ativas.
- `GET /categorias/{id}` — *(Público)* Detalhes de uma categoria.
- `POST /categorias` — *[categorias:escrever]* Criação de categoria.
- `PUT /categorias/{id}` — *[categorias:escrever]* Edição de categoria.
- `DELETE /categorias/{id}` — *[categorias:escrever]* Exclusão lógica/física.

### Produtos & Estoque (`/api/v1/produtos` e `/api/v1/estoque`)
- `GET /produtos` — *(Público)* Lista paginada com filtros por categoria, busca textual e ordenação. Inclui estoque atual e mínimo.
- `GET /produtos/{id}` — *(Público)* Detalhes completos do produto.
- `POST /produtos` — *[produtos:escrever]* Cadastro de produto com foto, categoria, tipo, estoque inicial e mínimo.
- `PUT /produtos/{id}` — *[produtos:escrever]* Atualização cadastral de produto.
- `DELETE /produtos/{id}` — *[produtos:escrever]* Remoção de produto.
- `GET /produtos/{id}/estoque` — *[produtos:ler]* Consulta de saldo em estoque e nível mínimo.
- `PUT /produtos/{id}/estoque` — *[estoque:gerenciar]* Ajuste manual de estoque (Entrada, Saída ou Ajuste).
- `GET /produtos/{id}/estoque/movimentos` — *[produtos:ler]* Extrato de movimentações de estoque do item.
- `GET /estoque/baixo` — *[estoque:gerenciar]* Relatório de produtos com estoque em nível crítico.

### Imagens (`/api/v1/imagens`)
- `POST /imagens` — *[produtos:escrever]* Upload multipart/form-data de imagens (máximo 5MB). Salva no Supabase Storage e retorna URL pública CDN.

### Endereços (`/api/v1/enderecos`)
- `GET /enderecos/cep/{cep}` — *(Público)* Consulta e autopreenchimento de logradouro via ViaCEP.

### CRM de Clientes (`/api/v1/clientes`)
- `GET /clientes` — *[clientes:ler]* Listagem de clientes com busca por nome/CPF e paginação.
- `GET /clientes/{id}` — *[clientes:ler]* Ficha detalhada do cliente e histórico.
- `POST /clientes` — *[clientes:escrever]* Cadastro de cliente pelo painel administrativo.
- `PUT /clientes/{id}` — *[clientes:escrever]* Atualização cadastral.
- `DELETE /clientes/{id}` — *[clientes:escrever]* Remoção de cliente.

### Carrinho de Compras (`/api/v1/carrinho`)
- `GET /carrinho` — Retorna o carrinho do cliente autenticado (itens, subtotais e cálculo do frete).
- `POST /carrinho/itens` — Adiciona produto ao carrinho (valida estoque disponível).
- `PATCH /carrinho/itens/{produtoId}` — Atualiza quantidade de um item.
- `DELETE /carrinho/itens/{produtoId}` — Remove item específico.
- `DELETE /carrinho` — Limpa todos os itens do carrinho.

### Pedidos (`/api/v1/pedidos`)
- `POST /pedidos/checkout` — Converte os itens do carrinho em um pedido formal. Executa débito de estoque atômico.
- `GET /pedidos` — *[pedidos:ler]* Listagem gerencial de pedidos com filtros de status e período.
- `GET /pedidos/{id}` — Consulta detalhes completos do pedido, itens, endereço e transações associadas.
- `POST /pedidos/{id}/etapas/avancar` — *[pedidos:avancar]* Faz a progressão controlada de etapa do pedido.
- `POST /pedidos/{id}/pagamento/concluir` — Finaliza formalmente o pagamento de um pedido aprovado.

### RBAC - Governança de Usuários e Acessos
- `GET /api/v1/permissions` — *[permissoes:gerenciar]* Lista todas as permissões do sistema.
- `GET /api/v1/permissions/{id}` — *[permissoes:gerenciar]* Detalhes de uma permissão.
- `POST /api/v1/permissions` — *[permissoes:gerenciar]* Criação de permissão personalizada.
- `PUT /api/v1/permissions/{id}` — *[permissoes:gerenciar]* Alteração de permissão.
- `DELETE /api/v1/permissions/{id}` — *[permissoes:gerenciar]* Exclusão de permissão.
- `GET /api/v1/roles` — *[roles:gerenciar]* Lista papéis configurados e permissões vinculadas.
- `GET /api/v1/roles/{id}` — *[roles:gerenciar]* Detalhes da role.
- `POST /api/v1/roles` — *[roles:gerenciar]* Criação de nova role.
- `PUT /api/v1/roles/{id}` — *[roles:gerenciar]* Atualização de dados da role.
- `PUT /api/v1/roles/{id}/permissions` — *[roles:gerenciar]* Sincronização em lote de permissões da role.
- `DELETE /api/v1/roles/{id}` — *[roles:gerenciar]* Remoção de role.
- `GET /api/v1/usuarios` — *[usuarios:gerenciar]* Lista operadores e usuários do sistema.
- `GET /api/v1/usuarios/{id}` — *[usuarios:gerenciar]* Detalhes de um usuário.
- `POST /api/v1/usuarios` — *[usuarios:gerenciar]* Criação de usuário com atribuição de roles.
- `PUT /api/v1/usuarios/{id}/roles` — *[usuarios:gerenciar]* Atualização de papéis do usuário.
- `PATCH /api/v1/usuarios/{id}/ativo` — *[usuarios:gerenciar]* Ativação ou inativação de conta.
- `DELETE /api/v1/usuarios/{id}` — *[usuarios:gerenciar]* Exclusão lógica/física.

### Relatórios (`/api/v1/relatorios`)
- `GET /api/v1/relatorios/vendas/resumo` — *[relatorios:ler]* Resumo executivo de faturamento por período, ticket médio e contagem de pedidos por status.

---

## 8. Convenções de Dados & Tratamento de Erros

### Enums na Serialização JSON
1. **JSON Body (Entrada e Saída):** Enums são serializados como **números inteiros** (sem conversor global para string):
   - `MetodoPagamento`: `0 = CartaoDebito`, `1 = CartaoCredito`, `2 = Pix`
   - `PedidoEtapa`: `0 = Criado`, `1 = Aprovado`, `2 = Pagamento`, `3 = PagamentoAprovado`, `4 = EnvioPendente`, `5 = Entregue`, `6 = Finalizado`, `-1 = Cancelado`
   - `StatusTransacao`: `0 = Pendente`, `1 = Aprovado`, `2 = Recusado`, `3 = Expirado`
   - `TipoProduto`: `0 = Simples`, `1 = Combo`
   - `TipoMovimentoEstoque`: `0 = Entrada`, `1 = Saida`, `2 = Ajuste`, `3 = Venda`
   - `EntregaTipo`: `0 = Transporte`, `1 = Retirada`
2. **Query Parameters:** Aceitam tanto o valor numérico quanto o nome amigável em string (case-insensitive, ex: `?tipo=simples`).

### Padrão ProblemDetails (RFC 7807)
Erros de validação e regras de negócio devolvem envelope padrão:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Estoque insuficiente para 'Hambúrguer Artesanal'. Disponível: 2, solicitado: 5.",
  "traceId": "0HN1234567890:00000001"
}
```

---

## 9. Variáveis de Ambiente & Configurações

| Variável / Chave | Finalidade | Exemplo de Configuração |
|------------------|------------|-------------------------|
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL | `Host=localhost;Port=5432;Database=gatums_db;Username=postgres;Password=...` |
| `Jwt__SecretKey` | Chave de assinatura HMAC-SHA256 (mínimo 32 chars) | `GatumsGourmetSuperSecretKey2026!Min32Chars` |
| `Jwt__Issuer` / `Jwt__Audience` | Emissor e público-alvo dos tokens | `GatumsGourmetApi` |
| `Jwt__AccessTokenMinutes` | Expiração do token de acesso | `15` |
| `Jwt__RefreshTokenDays` | Expiração do token de rotação | `7` |
| `MercadoPago__AccessToken` | Credencial secreta de produção/teste do Mercado Pago | `APP_USR-xxxxxx...` ou `TEST-xxxxxx...` |
| `MercadoPago__WebhookSecret` | Segredo para validação de assinatura IPN | `whsec_xxxxxx...` |
| `MercadoPago__BaseUrl` | URL base da API do gateway | `https://api.mercadopago.com` |
| `SupabaseStorage__ServiceUrl` | Endpoint S3 do Supabase | `https://<id>.supabase.co/storage/v1/s3` |
| `SupabaseStorage__PublicBaseUrl` | URL CDN pública do bucket de imagens | `https://<id>.supabase.co/storage/v1/object/public/gatums-imagens` |
| `SupabaseStorage__AccessKey` / `SecretKey` | Chaves de acesso ao S3 do Supabase | `...` |
| `Seq__ServerUrl` | Endereço do Seq para ingestão de logs | `http://localhost:5341` |

---

## 10. Diretrizes para Assistentes de IA (Instruções de Prompt)

Quando você (IA) estiver trabalhando neste repositório ou gerando código para ele, siga rigorosamente as seguintes convenções:

1. **Clean Architecture e Inversão de Dependências:**
   - Nunca referencie a camada `Infrastructure` ou `Api` de dentro de `Domain` ou `Application`.
   - Adicione novos contratos de serviços ou repositórios em `Domain/Repositories/` ou `Domain/Services/Interfaces/`.
   - Implemente os acessos a dados e clientes externos em `Infrastructure`.

2. **Result Pattern & Tratamento de Erros:**
   - Nunca lance exceções (`throw new Exception(...)`) para controlar regras de negócio.
   - Retorne sempre `Result.Ok()`, `Result.Fail("mensagem")`, `Result<T>.Ok(valor)` ou `Result<T>.Fail("mensagem")`.
   - Em Minimal APIs, trate a falha convertendo para `TypedResults.BadRequest(new ProblemDetails { Detail = result.Error })` ou código HTTP condizente (404, 403, 401).

3. **C# 13 & Estilo Moderno:**
   - Use declarações de namespace no escopo de arquivo (`namespace GatumsGourmet.X;`).
   - Use construtores primários (`public class MeuHandler(...) : IHandler`) sempre que fizer sentido.
   - Habilite referências nulas explícitas (`#nullable enable`).
   - Prefira imutabilidade para DTOs, registros de eventos e requisições (`public record Request(...)`).

4. **Migrações do Entity Framework Core:**
   - Ao alterar entidades de domínio que mapeiam para o banco de dados, crie uma migration dedicada em `GatumsGourmet.Infrastructure/Migrations/` e atualize o `GatumsDbContextModelSnapshot.cs`.
   - Utilize a convenção de nomenclatura de migrações com timestamp `YYYYMMDDHHMMSS_Descricao.cs`.

5. **Testes Unitários:**
   - Para cada novo UseCase ou Handler adicionado em `Application`, crie a suíte de testes correspondente em `tests/GatumsGourmet.UnitTests/Application/UseCases/...`.
   - Utilize Moq para criar mocks de interfaces e FluentAssertions para asserts legíveis.

6. **Sincronização de Documentação & Changelog:**
   - Toda adição de endpoint, alteração de contrato ou novo recurso de infraestrutura DEVE ser documentada em `docs/CONTEXT.md` e registrada em `docs/CHANGELOG.md` com data e detalhes das mudanças necessárias no front-end.

