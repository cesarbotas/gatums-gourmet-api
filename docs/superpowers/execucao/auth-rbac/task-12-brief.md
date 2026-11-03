# Task 12: CRUD de Roles e Permissions (use cases + endpoints)

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 127 unit + 82 integração verdes.

**Objetivo:** expor a gestão de roles e permissões via API (U2), protegida por permissão.

## Constraints globais
- net10.0, noBOM. Use cases em `Application/UseCases/V1/Roles/<UseCase>/` e `.../Permissions/<UseCase>/`. Padrão: Handler + Request + Response + interface no mesmo arquivo do Handler (estilo CreateCategoriaHandler). Endpoints em `Api/Endpoints/V1/RolesEndpoints.cs` e `PermissionsEndpoints.cs`.
- DI manual (registrar cada handler em ApplicationServiceCollectionExtensions).
- `RequirePermission` de `GatumsGourmet.Api.Authorization`.
- Repositórios disponíveis (Task 5): `IRoleRepository { GetByIdAsync (com permissões), GetByNomeAsync, ListAsync, AddAsync, Update, Remove, SetPermissionsAsync, CountUsuariosAsync, SaveChangesAsync }`, `IPermissionRepository { GetByIdAsync, GetByCodigoAsync, ListAsync, AddAsync, Update, Remove, SaveChangesAsync }`.
- Entidades: `Role { Id, Nome, Descricao?, Sistema, RolePermissions }`, `Permission { Id, Codigo, Descricao?, Recurso, Acao }`.
- Registrar os grupos no `EndpointRouteBuilderExtensions.cs` (ver como os outros são mapeados: `RolesEndpoints.Map(v1.MapGroup("/roles").WithTags("Roles"))` e idem Permissions).

## Use cases de Roles (`Application/UseCases/V1/Roles/`)

Shape de resposta compartilhado: criar `RoleResponse(int Id, string Nome, string? Descricao, bool Sistema, IReadOnlyList<string> Permissoes)` no nível do módulo (arquivo `Roles/RoleResponse.cs`, namespace `...V1.Roles`).

- **ListRoles/** `IListRolesHandler.Handle(ct) → Result<IReadOnlyList<RoleResponse>>` — lista todas as roles com seus códigos de permissão (via RolePermissions→Permission.Codigo).
- **GetRoleById/** `Handle(int id, ct) → Result<RoleResponse>` — Fail "Role não encontrada." se null.
- **CreateRole/** `CreateRoleRequest(string Nome, string? Descricao)`; valida Nome não vazio/≤100; Fail se já existe (GetByNomeAsync); cria `Role { Nome, Descricao, Sistema=false }`; retorna `CreateRoleResponse(int Id)`.
- **UpdateRole/** `UpdateRoleRequest(int Id, string Nome, string? Descricao)`; carrega por id; Fail se null; **se Sistema==true → Fail "Roles de sistema não podem ser alteradas."**; atualiza Nome/Descricao; Update + SaveChanges.
- **DeleteRole/** `Handle(int id, ct) → Result<bool>`; carrega; Fail se null; **se Sistema==true → Fail "Roles de sistema não podem ser removidas."**; **se CountUsuariosAsync>0 → Fail "Role possui usuários vinculados."**; Remove + SaveChanges.
- **SetRolePermissions/** `SetRolePermissionsRequest(int RoleId, IReadOnlyList<int> PermissionIds)`; carrega role; Fail se null; chama `SetPermissionsAsync(roleId, permissionIds, ct)` + SaveChanges; retorna Ok(true). (Permite trocar o conjunto de permissões de uma role, inclusive de sistema? A SDD diz que role de sistema não pode ser deletada/renomeada, mas o conjunto de permissões do Admin não deveria mudar — por segurança, **bloquear SetPermissions em role Sistema=true** com Fail "Permissões de roles de sistema são fixas." Registrar essa decisão no report.)

## Use cases de Permissions (`Application/UseCases/V1/Permissions/`)
Shape: `PermissionResponse(int Id, string Codigo, string? Descricao, string Recurso, string Acao)` em `Permissions/PermissionResponse.cs`.
CRUD completo (a SDD pediu, para backoffice futuro):
- **ListPermissions/** `→ Result<IReadOnlyList<PermissionResponse>>`.
- **GetPermissionById/** `Handle(int id) → Result<PermissionResponse>`.
- **CreatePermission/** `CreatePermissionRequest(string Codigo, string? Descricao, string Recurso, string Acao)`; valida Codigo não vazio/≤100 e formato (contém ':'); Fail se GetByCodigoAsync existe; cria; retorna `CreatePermissionResponse(int Id)`.
- **UpdatePermission/** `UpdatePermissionRequest(int Id, string? Descricao, string Recurso, string Acao)`; carrega; Fail se null; atualiza Descricao/Recurso/Acao (NÃO o Codigo — é a chave lógica); Update+SaveChanges.
- **DeletePermission/** `Handle(int id) → Result<bool>`; carrega; Fail se null; Remove+SaveChanges. (Nota: permissão deletada que ainda é usada em RequirePermission numa rota apenas fará aquela rota negar sempre — documentar na Task 16, não bloquear aqui.)

## Endpoints

### RolesEndpoints.cs — todas com `RequirePermission("roles:gerenciar")`
- GET `/` → ListRoles
- GET `/{id:int}` → GetRoleById
- POST `/` → CreateRole (201)
- PUT `/{id:int}` → UpdateRole (body UpdateRoleRequest com Id via `request with { Id = id }`)
- DELETE `/{id:int}` → DeleteRole
- PUT `/{id:int}/permissions` → SetRolePermissions (body com PermissionIds; `request with { RoleId = id }`)

### PermissionsEndpoints.cs — todas com `RequirePermission("permissoes:gerenciar")`
- GET `/` → ListPermissions
- GET `/{id:int}` → GetPermissionById
- POST `/` → CreatePermission (201)
- PUT `/{id:int}` → UpdatePermission
- DELETE `/{id:int}` → DeletePermission

## DI + roteamento
- Registrar todos os handlers novos em `ApplicationServiceCollectionExtensions`.
- Adicionar `RolesEndpoints.Map(v1.MapGroup("/roles").WithTags("Roles"));` e `PermissionsEndpoints.Map(v1.MapGroup("/permissions").WithTags("Permissions"));` no `EndpointRouteBuilderExtensions.MapV1Endpoints`.

## Testes de integração (`tests/.../V1/Roles/` e `.../Permissions/`)
Usar admin autenticado (CreateAdminClientAsync — tem roles:gerenciar e permissoes:gerenciar). Casos principais:
- Roles: criar role → 201; listar → contém a nova; GET by id; PUT permissions (vincular 2 permissões por id) → 200 e GET confirma; **DELETE role de sistema (buscar o id da role Admin via GET) → falha (400)**; criar role, deletar role vazia → 200.
- Cliente (sem permissão) em POST /roles → 403.
- Permissions: criar permissão (codigo "teste:acao") → 201; listar; deletar.
Para pegar ids de permissões para o SetRolePermissions, use GET /permissions.

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test tests/GatumsGourmet.IntegrationTests` toda verde (reporte contagem); unit verde. Investigue regressões.

## Commit
`feat: CRUD de roles e permissions via API`

## Não dispare subagentes. Bloqueie alteração/remoção/permissões de roles de sistema (Admin/Cliente).
