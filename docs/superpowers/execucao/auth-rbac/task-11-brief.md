# Task 11: Aplicar permissões nas rotas existentes (substituir policies legadas)

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 133 unit + 72 integração verdes.

**Objetivo:** trocar `RequireAuthorization("AdminOnly"/"ClienteOnly")` pelas permissões corretas (`RequirePermission("...")`) ou por `RequireAuthorization()` + ownership, conforme a matriz da SDD. Remover as policies legadas. Ajustar `IsAdmin`. Garantir que a suíte de integração continue verde.

## Constraints globais
- net10.0, noBOM. `RequirePermission` vem de `GatumsGourmet.Api.Authorization` (Task 10) — adicionar `using GatumsGourmet.Api.Authorization;` nos endpoints que o usarem.
- O admin de teste (CreateAdminClientAsync) tem a role Admin com TODAS as 12 permissões (seed da migration), então passa em qualquer RequirePermission. O cliente de teste tem role Cliente com 0 permissões → recebe 403 em rotas administrativas (comportamento esperado dos testes atuais de "ComoCliente_RetornaForbidden").

## Matriz a aplicar (da SDD seção 3)

### ProdutosEndpoints.cs
- GET / e GET /{id} → PÚBLICO (remover nada; já são públicos).
- POST / , PUT /{id} , DELETE /{id} → trocar `RequireAuthorization("AdminOnly")` por `RequirePermission("produtos:escrever")`.

### CategoriasEndpoints.cs
- GET / e GET /{id} → públicos (mantêm).
- POST / , PUT /{id} , DELETE /{id} → `RequirePermission("categorias:escrever")`.

### ClientesEndpoints.cs
- GET / → `RequirePermission("clientes:ler")`.
- POST / , PUT /{id} , DELETE /{id} → `RequirePermission("clientes:escrever")`.

### RelatoriosEndpoints.cs
- GET /vendas/resumo → `RequirePermission("relatorios:ler")`.

### PagamentosEndpoints.cs
- /opcoes, /iniciar, /{id}/cartao, /{id}/status, /{id}/pix/confirmar → eram "ClienteOnly"; viram `RequireAuthorization()` (apenas autenticado — são operações do próprio cliente; ownership não é verificável aqui sem refatorar handlers, então basta autenticado).
- POST /{id}/validar → `RequirePermission("pagamentos:validar")` (era AdminOnly).

### CarrinhoEndpoints.cs
- Todas as 5 rotas eram "ClienteOnly" → viram `RequireAuthorization()` (autenticado + ownership natural, pois o handler usa user.GetUserId()).

### PedidosEndpoints.cs
- POST /checkout → era "ClienteOnly" → `RequireAuthorization()` (ownership: usa user.GetUserId()).
- GET / → já é RequireAuthorization() (mantém). A lógica interna usa user.IsAdmin() para ver todos vs próprios.
- GET /{id} → já RequireAuthorization() (mantém; ownership no handler).
- POST /{id}/etapas/avancar → era RequireAuthorization() com actor derivado de IsAdmin → trocar para `RequirePermission("pedidos:avancar")`. ATENÇÃO: o handler deriva `actor = user.IsAdmin() ? Admin : Cliente`. Com RequirePermission, quem tem pedidos:avancar é Admin/Gerente — então `actor` deve ser Admin nesse contexto. Mantenha a derivação via IsAdmin() (Admin tem a permissão e IsInRole Admin); mas como Gerente também teria pedidos:avancar sem ser "Admin" na claim role... Para não mudar a regra de workflow agora, mantenha `actor = user.IsAdmin() ? PedidoActor.Admin : PedidoActor.Cliente`. (A semântica de workflow para Gerente é fora do escopo desta task — registrar como concern.)
- POST /{id}/pagamento/concluir → era "ClienteOnly" → `RequireAuthorization()`.

## IsAdmin (ClaimsPrincipalExtensions.cs)
Mantém `user.IsInRole("Admin")` (já corrigido na Task 6). Nenhuma mudança necessária — a menos que você julgue melhor basear em permissão. NÃO mude a assinatura; outros pontos (PedidosEndpoints) dependem de IsAdmin().

## Remover policies legadas (AuthenticationExtensions.cs)
Após migrar TODAS as rotas acima, remover:
```csharp
opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
opt.AddPolicy("ClienteOnly", p => p.RequireRole("Cliente"));
```
Deixar o bloco `AddAuthorization(opt => { })` vazio ou remover o lambda se não sobrar nada (mas manter `services.AddAuthorization()` para o pipeline). Garanta que nenhuma rota referencia mais "AdminOnly"/"ClienteOnly" (grep).

## Teste HTTP end-to-end de permissão (pointer da Task 10)
Em `tests/.../V1/Produtos/` ou um novo `PermissionEnforcementEndpointTests.cs`:
- Anônimo em POST /api/v1/produtos → 401.
- Cliente (sem permissão) em POST /api/v1/produtos → 403.
- Admin (com permissão) em POST /api/v1/produtos (com categoria válida) → 201.
(Muitos desses casos já existem como "ComoCliente_RetornaForbidden"/"ComoAdmin"; se já cobrem 401/403/200, aponte isso e adicione só o que faltar — provavelmente o caso anônimo 401.)

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test tests/GatumsGourmet.IntegrationTests` (container de pé) — TODA a suíte verde (os testes "ComoCliente_RetornaForbidden" devem continuar 403; "ComoAdmin" 201/200). Rode também unit. Reporte contagens. Investigue qualquer regressão — especialmente se algum teste de cliente em rota administrativa mudou de 403 para outro código.

## Commit
`refactor: proteger rotas por permissao em vez de role`

## Não dispare subagentes. Confirme com grep que não resta "AdminOnly"/"ClienteOnly" em nenhum lugar após a migração.
