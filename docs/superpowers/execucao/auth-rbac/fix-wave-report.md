# Fix-wave report — auth RBAC + refresh token (pós review final)

Branch: feature/AjusteLoginToken · Projeto: API .NET 10 (Clean Architecture)

## Fix 1 (#2) — PedidosEndpoints: autorização por permissão (`pedidos:ler`) em vez de role nominal

Em `src/GatumsGourmet.Api/Endpoints/V1/PedidosEndpoints.cs`, os 3 usos de `user.IsAdmin()` foram trocados por `user.HasPermission("pedidos:ler")`:

1. `GET /` — `Guid? filtro = user.HasPermission("pedidos:ler") ? null : user.GetUserId();`
2. `GET /{id}` — 3º arg do handler: `handler.Handle(id, user.GetUserId(), user.HasPermission("pedidos:ler"), ct)`
3. `POST /{id}/etapas/avancar` — `var actor = user.HasPermission("pedidos:ler") ? PedidoActor.Admin : PedidoActor.Cliente;`

O método `IsAdmin()` em `ClaimsPrincipalExtensions` NÃO foi alterado (continua `IsInRole("Admin")`, pode ter outros usos). Com isso, um Gerente que tenha `pedidos:ler`/`pedidos:avancar` mas não a role nominal "Admin" passa a enxergar todos os pedidos e agir como operador administrativo, alinhando o endpoint ao RBAC. Comportamento para o admin de teste (que tem todas as permissões, incl. `pedidos:ler`) é idêntico ao anterior.

## Fix 2 (#7) — CreateUsuarioHandler: criação de usuário + roles atômica

Em `src/GatumsGourmet.Application/UseCases/V1/Usuarios/CreateUsuario/CreateUsuarioHandler.cs`, os dois `SaveChangesAsync` (um para o usuário, outro para as roles) foram unificados num único `SaveChangesAsync`.

**Abordagem usada: SaveChanges único (não precisou de transação explícita).**

Sequência: `AddAsync(usuario)` → (se houver roleIds) `SetRolesAsync(usuario.Id, roleIds)` → `SaveChangesAsync()` uma única vez.

Validação feita: `SetRolesAsync` (em `UsuarioRepository`) apenas manipula o DbSet (`RemoveRange` + `AddRangeAsync`) e NÃO chama `SaveChanges`. Ele vincula via FK escalar `UsuarioId` em novos `UsuarioRole`. Como o `Usuario` já está sendo rastreado pelo `AddAsync` e há relacionamento FK, o EF ordena os inserts corretamente (usuário antes dos `UsuarioRoles`) dentro do mesmo `SaveChanges`/transação. O teste de integração de CreateUsuario (com roles) confirmou que funciona — não foi necessário `IExecutionStrategy`/transação explícita.

Benefício: elimina o estado inconsistente de usuário criado sem roles caso o segundo SaveChanges falhasse.

## Build + suítes

- `dotnet build GatumsGourmet.sln`: **0 erros** (apenas avisos pré-existentes: NETStandardCompat no Infrastructure + warnings de analisador nos testes).
- UnitTests: **143 aprovados, 0 com falha** (Total 143).
- IntegrationTests: **95 aprovados, 0 com falha** (Total 95, container Postgres 5433 de pé).
- Subconjunto específico Pedido/Usuario na integração: **22 aprovados, 0 com falha** — confirma ListPedidos, GetPedidoById, AvancarPedidoEtapa e CreateUsuario passando.

## Arquivos alterados

- `src/GatumsGourmet.Api/Endpoints/V1/PedidosEndpoints.cs` (+3 −3)
- `src/GatumsGourmet.Application/UseCases/V1/Usuarios/CreateUsuario/CreateUsuarioHandler.cs` (+6 −4)

## Auto-revisão

- Fix 1: exatamente os 3 pontos trocados; `RequirePermission("pedidos:avancar")` na rota de avançar mantido; `IsAdmin()` preservado no extension. Mudança mínima, sem refatoração extra.
- Fix 2: um único SaveChanges; comentário explicativo adicionado; nenhuma API nova no repositório. Lógica de validação de e-mail duplicado e de existência de roles intacta.
- Sem alterações fora dos 2 fixes.

## Concerns

- Nenhum bloqueante. Observação de design: `pedidos:ler` é usado como marcador de "operador administrativo" para definir o `PedidoActor` no avançar de etapa. Funciona porque Admin e Gerente têm essa permissão, mas acopla o conceito de "actor administrativo" à permissão de leitura. Se futuramente surgir um perfil com `pedidos:avancar` mas sem `pedidos:ler`, convém introduzir um marcador dedicado. Fora do escopo deste fix-wave.
