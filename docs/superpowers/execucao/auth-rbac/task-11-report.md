# Task 11 — Aplicar permissões nas rotas (substituir policies legadas)

Branch: feature/AjusteLoginToken · Projeto: GatumsGourmet (.NET 10, Clean Architecture)

## Objetivo
Trocar `RequireAuthorization("AdminOnly"/"ClienteOnly")` por `RequirePermission("...")` ou
`RequireAuthorization()` conforme a matriz da SDD, remover as policies legadas por papel e
garantir a suíte verde.

## Rotas migradas por arquivo

### ProdutosEndpoints.cs (`+ using GatumsGourmet.Api.Authorization`)
- GET `/`, GET `/{id}` → públicos (inalterados).
- POST `/`, PUT `/{id}`, DELETE `/{id}` → `RequirePermission("produtos:escrever")`.

### CategoriasEndpoints.cs (`+ using ...Authorization`)
- GET `/`, GET `/{id}` → públicos (inalterados).
- POST `/`, PUT `/{id}`, DELETE `/{id}` → `RequirePermission("categorias:escrever")`.

### ClientesEndpoints.cs (`+ using ...Authorization`)
- GET `/` → `RequirePermission("clientes:ler")`.
- POST `/`, PUT `/{id}`, DELETE `/{id}` → `RequirePermission("clientes:escrever")`.

### RelatoriosEndpoints.cs (`+ using ...Authorization`)
- GET `/vendas/resumo` → `RequirePermission("relatorios:ler")`.

### PagamentosEndpoints.cs (`+ using ...Authorization`)
- `/opcoes`, `/iniciar`, `/{id}/cartao`, `/{id}/status`, `/{id}/pix/confirmar`
  (eram `ClienteOnly`) → `RequireAuthorization()` (apenas autenticado; operações do próprio
  cliente, ownership permanece no handler).
- POST `/{id}/validar` (era `AdminOnly`) → `RequirePermission("pagamentos:validar")`.

### CarrinhoEndpoints.cs (sem novo using — só usa RequireAuthorization())
- As 5 rotas (eram `ClienteOnly`) → `RequireAuthorization()` (ownership natural via
  `user.GetUserId()` no handler).

### PedidosEndpoints.cs (`+ using ...Authorization`)
- POST `/checkout` (era `ClienteOnly`) → `RequireAuthorization()`.
- GET `/` e GET `/{id}` → `RequireAuthorization()` (inalterados; lógica interna usa
  `user.IsAdmin()` / ownership no handler).
- POST `/{id}/etapas/avancar` (era `RequireAuthorization()`) → `RequirePermission("pedidos:avancar")`.
  A derivação `actor = user.IsAdmin() ? Admin : Cliente` foi mantida (ver Concerns).
- POST `/{id}/pagamento/concluir` (era `ClienteOnly`) → `RequireAuthorization()`.

## Remoção das policies legadas
Em `AuthenticationExtensions.cs` o bloco `AddAuthorization(opt => { AddPolicy("AdminOnly"...);
AddPolicy("ClienteOnly"...); })` foi substituído por `services.AddAuthorization();` (pipeline
mantido). O provider dinâmico (`PermissionPolicyProvider`) e o handler continuam registrados.
O comentário do `PermissionPolicyProvider.cs` que citava "AdminOnly"/"ClienteOnly" como exemplos
foi atualizado para "fallback para policies nomeadas convencionais".

## IsAdmin
Inalterado (`user.IsInRole("Admin")`, corrigido na Task 6). PedidosEndpoints depende disso para
a derivação de actor; assinatura preservada.

## Grep de confirmação
- `AdminOnly|ClienteOnly` em `src/**/*.cs` → **nenhum match**.
- `RequireAuthorization("..."` (policy nomeada por string) em `src/**/*.cs` → **nenhum match**.
- Em testes resta apenas `PermissionPolicyProviderTests.cs` usando `"AdminOnly"` como **nome
  arbitrário** de policy local para exercitar o caminho de fallback do provider — não é a policy
  legada da aplicação; é um teste legítimo e foi mantido.
- Demais ocorrências (docs/, Postman, plan/spec) são documentação, fora do escopo de código.

## Teste adicionado
- Novo `tests/GatumsGourmet.IntegrationTests/Api/Authorization/PermissionEnforcementEndpointTests.cs`
  (pointer da Task 10): rota protegida por permissão (`POST /api/v1/produtos`,
  `produtos:escrever`) → anônimo **401**, cliente sem permissão **403**, admin com permissão
  **201**. Reutiliza `CreateProdutoRequestFixture`. noBOM confirmado.
- Os três casos canônicos (401/403/201) já existiam em `CreateProdutoEndpointTests`; o novo teste
  consolida a dimensão de permissão explicitamente num só lugar, sem duplicar comportamento.

## Build + suítes
- `dotnet build GatumsGourmet.sln` → **0 erros** (apenas warnings pré-existentes: NU1902/NU1903
  do System.Security.Cryptography.Pkcs, MSB3277 EF Core, CS0105 usings duplicados em unit tests).
- `dotnet test tests/GatumsGourmet.IntegrationTests` → **82 aprovados, 0 falhas** (container
  Postgres 5433 de pé).
- `dotnet test tests/GatumsGourmet.UnitTests` → **127 aprovados, 0 falhas**.

## Regressão investigada
Na primeira execução falhou **1** teste:
`ListOpcoesPagamentoEndpointTests.Opcoes_ComoAdmin_RetornaForbidden`.
- Causa raiz: o teste assumia a semântica antiga (rota `ClienteOnly` → admin, sem a role Cliente,
  recebia 403). Com a migração para `RequireAuthorization()` a rota exige apenas autenticação, e um
  admin autenticado agora recebe **200** — comportamento correto e previsto pela matriz da SDD.
- Correção (não-mascaramento): o teste foi atualizado para `Opcoes_ComoAdmin_RetornaOk` (admin
  autenticado → 200 com 3 opções) e adicionado `Opcoes_SemAutenticacao_RetornaUnauthorized`
  (anônimo → 401). A premissa obsoleta "admin proibido em rota de self-service" deixou de valer sob
  o novo modelo; não houve alteração de expectativa para esconder falha real.
- Todos os testes `ComoCliente_RetornaForbidden` em rotas administrativas continuaram **403** como
  esperado.

## Arquivos alterados
- `src/GatumsGourmet.Api/Endpoints/V1/ProdutosEndpoints.cs`
- `src/GatumsGourmet.Api/Endpoints/V1/CategoriasEndpoints.cs`
- `src/GatumsGourmet.Api/Endpoints/V1/ClientesEndpoints.cs`
- `src/GatumsGourmet.Api/Endpoints/V1/RelatoriosEndpoints.cs`
- `src/GatumsGourmet.Api/Endpoints/V1/PagamentosEndpoints.cs`
- `src/GatumsGourmet.Api/Endpoints/V1/CarrinhoEndpoints.cs`
- `src/GatumsGourmet.Api/Endpoints/V1/PedidosEndpoints.cs`
- `src/GatumsGourmet.Api/Extensions/AuthenticationExtensions.cs`
- `src/GatumsGourmet.Api/Authorization/PermissionPolicyProvider.cs` (comentário)
- `tests/.../Pagamentos/ListOpcoesPagamento/ListOpcoesPagamentoEndpointTests.cs` (regressão prevista)
- `tests/.../Api/Authorization/PermissionEnforcementEndpointTests.cs` (novo)

## Auto-revisão
- Matriz da SDD aplicada 1:1; nenhuma rota ficou sem proteção prevista.
- `using GatumsGourmet.Api.Authorization` adicionado só onde `RequirePermission` é usado
  (Produtos, Categorias, Clientes, Relatorios, Pagamentos, Pedidos). Carrinho não precisou.
- Pipeline de autorização preservado (`AddAuthorization()` + provider dinâmico + handler).
- Build 0 erros; unit 127/127; integração 82/82.

## Concerns
- **Actor Gerente em `pedidos:avancar`**: a derivação `actor = user.IsAdmin() ? Admin : Cliente`
  foi mantida. Com o modelo por permissão, um futuro papel Gerente teria `pedidos:avancar` sem ser
  `IsInRole("Admin")`, então seria classificado como `PedidoActor.Cliente` no workflow — o que pode
  não refletir a intenção de negócio. A semântica de workflow para Gerente está fora do escopo desta
  task; registrado como dívida para uma task futura (ex.: derivar o actor por permissão em vez de
  role).
- **Ownership em Pagamentos/Carrinho/Pedidos**: as rotas `RequireAuthorization()` dependem da
  checagem de ownership no handler (`user.GetUserId()`); não há verificação adicional na borda. Isso
  segue a decisão da SDD, mas convém confirmar que todos os handlers de fato filtram pelo usuário.
- Warnings de vulnerabilidade (NU1902/NU1903) em `System.Security.Cryptography.Pkcs` 9.0.0 são
  pré-existentes e fora do escopo.
