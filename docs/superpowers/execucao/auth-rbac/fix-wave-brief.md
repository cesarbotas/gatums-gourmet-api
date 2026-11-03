# Fix-wave (pós review final): alinhar autorização de pedidos (#2) + transação em CreateUsuario (#7)

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 143 unit + 95 integração verdes. Todas as 16 tasks completas; este é o único fix-wave pós review final da branch.

## Fix 1 (#2) — PedidosEndpoints: autorização por permissão em vez de role nominal
Hoje `PedidosEndpoints.cs` usa `user.IsAdmin()` (= `IsInRole("Admin")`) em 3 pontos. Isso trata um futuro Gerente (que tem `pedidos:ler`/`pedidos:avancar` mas não a role nominal "Admin") como cliente comum. Alinhar ao RBAC: "quem enxerga todos os pedidos / age como operador" = quem tem a permissão `pedidos:ler`.

Trocar nos 3 pontos de `PedidosEndpoints.cs` (o `using GatumsGourmet.Api.Extensions;` e `GatumsGourmet.Api.Authorization;` já estão no arquivo; `HasPermission` está em ClaimsPrincipalExtensions, Task 9):
1. `GET /` — `Guid? filtro = user.IsAdmin() ? null : user.GetUserId();` → `Guid? filtro = user.HasPermission("pedidos:ler") ? null : user.GetUserId();`
2. `GET /{id}` — `handler.Handle(id, user.GetUserId(), user.IsAdmin(), ct)` → `handler.Handle(id, user.GetUserId(), user.HasPermission("pedidos:ler"), ct)`. (O 3º parâmetro do handler é "isAdmin/podeVerTodos" — passar HasPermission("pedidos:ler").)
3. `POST /{id}/etapas/avancar` — `var actor = user.IsAdmin() ? PedidoActor.Admin : PedidoActor.Cliente;` → `var actor = user.HasPermission("pedidos:ler") ? PedidoActor.Admin : PedidoActor.Cliente;`. (A rota já exige `pedidos:avancar`; usamos `pedidos:ler` como marcador de "operador administrativo", que Admin e Gerente têm. Assim o actor fica Admin para operadores e Cliente para o dono do pedido.)

NÃO altere o método `IsAdmin()` em ClaimsPrincipalExtensions (pode ter outros usos; a semântica "é admin" continua válida). Só troque os usos DENTRO de PedidosEndpoints.

Observação: isso NÃO afeta os testes de integração de pedidos existentes, porque o admin de teste tem TODAS as permissões (incluindo pedidos:ler), então continua vendo todos os pedidos e agindo como Admin — comportamento idêntico ao de antes para o admin. Rode os testes para confirmar.

## Fix 2 (#7) — CreateUsuario: persistir usuário + roles numa única transação
Hoje `CreateUsuarioHandler` faz dois `SaveChangesAsync` (um para o usuário, outro para as roles). Se o segundo falhar, o usuário fica sem roles. Tornar atômico.

Abordagem SIMPLES e segura (sem adicionar API nova ao repositório): adicionar o usuário e vincular as roles ANTES de um único SaveChanges. Como `SetRolesAsync` precisa do UsuarioId e o usuário já tem Id gerado no app (Guid.NewGuid()), dá para:
- `await usuarios.AddAsync(usuario, ct);` (sem SaveChanges ainda)
- `if (roleIds.Count > 0) await usuarios.SetRolesAsync(usuario.Id, roleIds, ct);`
- `await usuarios.SaveChangesAsync(ct);` (um só — EF persiste o usuário e os UsuarioRoles na mesma transação do SaveChanges)

VERIFIQUE que `SetRolesAsync` (em UsuarioRepository) não dá SaveChanges internamente (pelo brief da Task 5, ele NÃO dá — só manipula o DbSet). Se SetRolesAsync adicionar UsuarioRole via `db.UsuarioRoles.Add(...)` para um UsuarioId que ainda não está persistido mas está sendo trackeado pelo AddAsync, o EF resolve a ordem de insert por causa das FKs/navegação. Se houver problema de ordem (FK UsuarioId antes do usuário existir), a alternativa é manter o AddAsync+SaveChanges do usuário e envolver os dois SaveChanges numa transação EF explícita — mas PREFIRA a abordagem do SaveChanges único se funcionar. Teste para confirmar que criar usuário com roles continua funcionando (o teste de integração da Task 13 cobre isso).

Se a abordagem de SaveChanges único não funcionar por causa da ordem de FK, documente e use `IExecutionStrategy`/transação; mas valide com o teste.

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test tests/GatumsGourmet.UnitTests` + `dotnet test tests/GatumsGourmet.IntegrationTests` TODOS verdes (container de pé). Reporte contagens. Em especial, confirme que os testes de pedidos (ListPedidos, GetPedidoById, AvancarPedidoEtapa) e de usuários (CreateUsuario) continuam passando.

## Commit
`fix: autorizacao de pedidos por permissao e criacao de usuario atomica`

## Não dispare subagentes. Mudanças mínimas e focadas nos 2 fixes. Não refatore além disso.
