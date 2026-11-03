# Autenticação com RBAC e Refresh Token — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir o JWT único de 8h por access token curto (15 min) + refresh token rotativo (7 dias) com detecção de reúso, e trocar o controle de acesso por enum (Admin/Cliente) por RBAC completo com permissões administrável via API.

**Architecture:** Clean Architecture (.NET 10). Roles/permissões/refresh tokens viram tabelas no Postgres (Domain entities + EF configs + repositories). Access token stateless carrega claim `perm[]`. Autorização por policy provider dinâmico (`RequirePermission`). Ownership continua verificado no handler. Migração transacional com data migration do enum legado; relogin no deploy.

**Tech Stack:** .NET 10, EF Core (Npgsql), FluentValidation, BCrypt.Net, JwtSecurityTokenHandler (HS256), xUnit + Moq + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-10-21-auth-rbac-refresh-token-design.md`

## Global Constraints

- Target framework `net10.0`; manter padrões existentes do repo.
- **Uma classe por arquivo**, agrupadas por pasta de responsabilidade. Interfaces de repositório em `Domain/Repositories/`; interfaces de service em `Domain/Services/Interfaces/`. Uma interface/classe por arquivo.
- Use cases em `Application/UseCases/V1/<Modulo>/<UseCase>/` (Handler + Interface + Request + Response, 1 classe por arquivo), seguindo o padrão existente.
- DI é **manual** em `ApplicationServiceCollectionExtensions` e `InfrastructureServiceCollectionExtensions` (sem scanning de handlers). Cada handler/repo novo precisa ser registrado manualmente.
- Arquivos `.cs` com **BOM UTF-8** (padrão do repo); preservar acentuação.
- Access token 15 min, refresh 7 dias, retenção 30 dias, cleanup 12h — todos de `appsettings` (`Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays`, `Auth:RefreshTokenRetentionDays`, `Auth:CleanupIntervalHours`).
- Permissões no formato `recurso:acao`. Claim de permissão no token = `perm` (múltiplos valores).
- Tudo em `/api/v1` — sem V2.

## Review Focus

- **Refresh reusado após rotação** → deve revogar a família inteira e retornar 401, não renovar. (Task 8)
- **Usuário desativado (`Ativo=false`)** → login e refresh devem falhar com 401, mesmo com senha/refresh válidos. (Task 7, Task 8)
- **Remoção do último admin** (tirar role Admin / desativar / excluir o último admin ativo) → deve ser bloqueado com erro de regra de negócio. (Task 13)
- **Token sem claim `perm` exigida** (ou permissão ausente) → rota protegida retorna 403, não 500. (Task 10)
- **Refresh expirado** → 401, e o registro continua elegível para purga. (Task 8, Task 14)

---

## File Structure

**Domain (`src/GatumsGourmet.Domain`)**
- `Entities/Auth/Role.cs`, `Permission.cs`, `RolePermission.cs`, `UsuarioRole.cs`, `RefreshToken.cs` (criar); `Usuario.cs` (modificar)
- `Enums/Auth/UserRole.cs` (remover ao fim da migração de código)
- `Repositories/IRoleRepository.cs`, `IPermissionRepository.cs`, `IRefreshTokenStore.cs` (criar); `IUsuarioRepository.cs` (ampliar)
- `Services/Interfaces/IRefreshTokenGenerator.cs`, `IPermissionResolver.cs` (criar); `IJwtTokenGenerator.cs` (modificar)

**Infrastructure (`src/GatumsGourmet.Infrastructure`)**
- `Services/Auth/JwtTokenGenerator.cs`, `RefreshTokenGenerator.cs`, `BcryptPasswordHasher.cs`, `PermissionResolver.cs` (criar/mover — quebrar `Services/InfrastructureServices.cs`)
- `Data/Configurations/Auth/RoleConfiguration.cs`, `PermissionConfiguration.cs`, `RolePermissionConfiguration.cs`, `UsuarioRoleConfiguration.cs`, `RefreshTokenConfiguration.cs` (criar); `UsuarioConfiguration.cs` (modificar)
- `Data/Repositories/Auth/RoleRepository.cs`, `PermissionRepository.cs`, `RefreshTokenStore.cs` (criar); `UsuarioRepository.cs` (ampliar)
- `Data/GatumsDbContext.cs` (adicionar DbSets)
- `Data/DbSeeder.cs` (ajustar seed do admin)
- `Migrations/<timestamp>_AuthRbacRefreshToken.cs` (gerar via EF)
- `InfrastructureServiceCollectionExtensions.cs` (registrar novos serviços/repos)

**Application (`src/GatumsGourmet.Application`)**
- `UseCases/V1/Auth/` — `Login/` (modificar), `RefreshToken/`, `Logout/`, `LogoutAll/` (criar)
- `UseCases/V1/Roles/` — CreateRole, ListRoles, GetRoleById, UpdateRole, DeleteRole, SetRolePermissions
- `UseCases/V1/Permissions/` — ListPermissions, GetPermissionById, CreatePermission, UpdatePermission, DeletePermission
- `UseCases/V1/Usuarios/` — CreateUsuario, ListUsuarios, GetUsuarioById, SetUsuarioRoles, SetUsuarioAtivo, DeleteUsuario
- `UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/`
- `ApplicationServiceCollectionExtensions.cs` (registrar handlers)

**Api (`src/GatumsGourmet.Api`)**
- `Authorization/PermissionRequirement.cs`, `PermissionAuthorizationHandler.cs`, `PermissionPolicyProvider.cs`, `RequirePermissionExtensions.cs` (criar)
- `Endpoints/V1/AuthEndpoints.cs` (modificar), `RolesEndpoints.cs`, `PermissionsEndpoints.cs`, `UsuariosEndpoints.cs` (criar)
- `Endpoints/V1/ProdutosEndpoints.cs`, `CategoriasEndpoints.cs`, `ClientesEndpoints.cs`, `PedidosEndpoints.cs`, `PagamentosEndpoints.cs`, `RelatoriosEndpoints.cs` (trocar policy→permission)
- `Extensions/AuthenticationExtensions.cs` (remover policies legadas, registrar policy provider)
- `Extensions/ClaimsPrincipalExtensions.cs` (helpers de permissão)
- `Extensions/EndpointRouteBuilderExtensions.cs` (mapear grupos novos)
- `BackgroundServices/RefreshTokenCleanupService.cs` (criar)
- `Program.cs` (registrar hosted service e policy provider)

**Docs**
- `docs/adr/0001..0004` , `docs/sql/seed-roles-permissions.sql`, `docs/Auth/Guia-Permissoes.md`, `docs/Auth/Guia-de-Uso.md`, `docs/Site/API-Reference.md` (atualizar)

---

## Task 1: Entidades de domínio do RBAC e refresh token

**Files:**
- Create: `Domain/Entities/Auth/Role.cs`, `Permission.cs`, `RolePermission.cs`, `UsuarioRole.cs`, `RefreshToken.cs`
- Modify: `Domain/Entities/Auth/Usuario.cs` (remover `Role`, adicionar `Ativo`, navegação `ICollection<UsuarioRole> UsuarioRoles`)
- Test: `tests/GatumsGourmet.UnitTests/Domain/Entities/Auth/RefreshTokenTests.cs`

**Interfaces:**
- Produces: `Role { int Id; string Nome; string? Descricao; bool Sistema; ICollection<RolePermission> RolePermissions }`, `Permission { int Id; string Codigo; string? Descricao; string Recurso; string Acao }`, `RolePermission { int RoleId; int PermissionId }`, `UsuarioRole { Guid UsuarioId; int RoleId }`, `RefreshToken { Guid Id; Guid UsuarioId; string TokenHash; Guid FamiliaId; DateTime ExpiraEm; DateTime CriadoEm; bool Revogado; DateTime? RevogadoEm; string? SubstituidoPorTokenHash; string? MotivoRevogacao; string? CriadoPorIp }`. `Usuario` ganha `bool Ativo` e `ICollection<UsuarioRole> UsuarioRoles`.

- [ ] **Step 1: Escrever teste** `RefreshTokenTests` com um teste `Novo_refresh_token_nao_nasce_revogado` que instancia `RefreshToken` e assevera `Revogado == false` e `RevogadoEm == null`. (Fixa a invariante default usada na detecção de reúso.)
- [ ] **Step 2: Rodar o teste e ver falhar** (tipo não existe). Run: `dotnet test tests/GatumsGourmet.UnitTests --filter RefreshTokenTests`
- [ ] **Step 3: Criar as 5 entidades** (1 arquivo cada) com as propriedades do bloco Interfaces. Modificar `Usuario.cs`: remover a propriedade `Role` e o `using ...Enums.Auth`, adicionar `bool Ativo` e `ICollection<UsuarioRole> UsuarioRoles = []`.
- [ ] **Step 4: Rodar o teste e ver passar.**
- [ ] **Step 5: Commit** `feat: entidades de RBAC e refresh token no domain`

> Nota: `Usuario.cs` sem a propriedade `Role` quebra a compilação de `LoginHandler`, `RegisterUserHandler`, `GetCurrentUserHandler`, `DbSeeder` e `UsuarioConfiguration` — esses são corrigidos nas tasks seguintes; a solução só volta a compilar ao fim da Task 7.

---

## Task 2: Interfaces de repositório e service (Domain)

**Files:**
- Create: `Domain/Repositories/IRoleRepository.cs`, `IPermissionRepository.cs`, `IRefreshTokenStore.cs`
- Modify: `Domain/Repositories/IUsuarioRepository.cs`, `Domain/Services/Interfaces/IJwtTokenGenerator.cs`
- Create: `Domain/Services/Interfaces/IRefreshTokenGenerator.cs`, `IPermissionResolver.cs`

**Interfaces:**
- Produces:
  - `IRoleRepository`: `GetByIdAsync(int)`, `GetByNomeAsync(string)`, `ListAsync(ct)`, `AddAsync(Role)`, `Update(Role)`, `Remove(Role)`, `SetPermissionsAsync(int roleId, IEnumerable<int> permissionIds, ct)`, `CountUsuariosAsync(int roleId, ct)`, `SaveChangesAsync`.
  - `IPermissionRepository`: `GetByIdAsync(int)`, `GetByCodigoAsync(string)`, `ListAsync(ct)`, `AddAsync`, `Update`, `Remove`, `SaveChangesAsync`.
  - `IRefreshTokenStore`: `AddAsync(RefreshToken)`, `GetByHashAsync(string tokenHash, ct)`, `RevokeFamilyAsync(Guid familiaId, string motivo, DateTime em, ct)`, `RevokeAllForUserAsync(Guid usuarioId, string motivo, DateTime em, ct)`, `DeleteExpiredAsync(DateTime cutoff, ct)`, `SaveChangesAsync`.
  - `IRefreshTokenGenerator`: `(string PlainToken, string Hash) Generate()`; `string Hash(string plainToken)`.
  - `IPermissionResolver`: `Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissoes)> ResolveAsync(Guid usuarioId, ct)`.
  - `IJwtTokenGenerator` (modificado): `string Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissoes)`.
  - `IUsuarioRepository` (ampliado): adicionar `ListAsync(ct)`, `GetByIdComRolesAsync(Guid, ct)`, `SetRolesAsync(Guid usuarioId, IEnumerable<int> roleIds, ct)`, `SetAtivoAsync(Guid usuarioId, bool ativo, ct)`, `CountAdminsAtivosAsync(ct)`.

- [ ] **Step 1: Criar/editar as interfaces** acima (1 por arquivo). São contratos — sem teste próprio; a compilação é validada nas tasks que implementam.
- [ ] **Step 2: Compilar o Domain.** Run: `dotnet build src/GatumsGourmet.Domain`. Expected: SUCCESS (Domain não referencia as impls).
- [ ] **Step 3: Commit** `feat: interfaces de repositorio e service para RBAC/refresh`

---

## Task 3: Quebrar InfrastructureServices e mover JwtTokenGenerator/BcryptPasswordHasher

**Files:**
- Create: `Infrastructure/Services/Auth/JwtTokenGenerator.cs`, `BcryptPasswordHasher.cs`, `RefreshTokenGenerator.cs`
- Create: `Infrastructure/Services/SystemClock.cs` (se hoje estiver junto no arquivo monolítico)
- Create: `Infrastructure/Services/Pagamentos/MockPaymentGateway.cs`, `MockPixProvider.cs` (mover os mocks para manter 1 classe por arquivo)
- Modify/Delete: `Infrastructure/Services/InfrastructureServices.cs` (esvaziar; remover ao fim)
- Modify: `Infrastructure/InfrastructureServiceCollectionExtensions.cs` (atualizar namespaces/registro)
- Test: `tests/GatumsGourmet.UnitTests/Infrastructure/Auth/JwtTokenGeneratorTests.cs`, `RefreshTokenGeneratorTests.cs`

**Interfaces:**
- Consumes: `IJwtTokenGenerator`, `IRefreshTokenGenerator` (Task 2).
- Produces: `JwtTokenGenerator` emite JWT com claims `sub`, `email`, um `role` por role, um `perm` por permissão, `jti`, `exp` (lê `Jwt:AccessTokenMinutes`, default 15). `RefreshTokenGenerator.Generate()` retorna token aleatório de 32 bytes base64url + seu hash SHA-256; `Hash(plain)` é determinístico.

- [ ] **Step 1: Escrever testes.** `JwtTokenGeneratorTests.Token_contem_claims_perm_e_role`: gera token com roles/permissões e, lendo via `JwtSecurityTokenHandler`, assevera que há uma claim `perm` por permissão e `role` por role. `RefreshTokenGeneratorTests.Hash_do_mesmo_token_e_estavel_e_plain_difere_do_hash`.
- [ ] **Step 2: Rodar e ver falhar.** Run: `dotnet test tests/GatumsGourmet.UnitTests --filter "JwtTokenGeneratorTests|RefreshTokenGeneratorTests"`
- [ ] **Step 3: Mover e implementar.** Mover `BcryptPasswordHasher` e os mocks de pagamento para arquivos próprios (sem alterar lógica). Reescrever `JwtTokenGenerator` com a nova assinatura (claim por role e por permissão; expiração de config). Criar `RefreshTokenGenerator` (RNG `RandomNumberGenerator.GetBytes(32)` → base64url; hash SHA-256 hex). Atualizar os `using`/registro no `InfrastructureServiceCollectionExtensions`.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `refactor: separar services de infra e novo JwtTokenGenerator com claims de permissao`

---

## Task 4: Configurações EF e DbSets

**Files:**
- Create: `Infrastructure/Data/Configurations/Auth/RoleConfiguration.cs`, `PermissionConfiguration.cs`, `RolePermissionConfiguration.cs`, `UsuarioRoleConfiguration.cs`, `RefreshTokenConfiguration.cs`
- Modify: `Infrastructure/Data/Configurations/Auth/UsuarioConfiguration.cs` (remover `HasConversion` de `Role`; mapear `Ativo`; navegação `UsuarioRoles`)
- Modify: `Infrastructure/Data/GatumsDbContext.cs` (DbSets `Roles`, `Permissions`, `RolePermissions`, `UsuarioRoles`, `RefreshTokens`)
- Test: (coberto na Task 6, integração de migração)

**Interfaces:**
- Produces: índices únicos `Role.Nome`, `Permission.Codigo`, `RefreshToken.TokenHash`; PKs compostas em `RolePermission (RoleId,PermissionId)` e `UsuarioRole (UsuarioId,RoleId)`; FKs com cascade apropriado (UsuarioRole/RolePermission cascade; RefreshToken cascade por usuário).

- [ ] **Step 1: Criar as 5 configs** + editar `UsuarioConfiguration` + adicionar DbSets.
- [ ] **Step 2: Compilar Infrastructure.** Run: `dotnet build src/GatumsGourmet.Infrastructure`. Expected: SUCCESS.
- [ ] **Step 3: Commit** `feat: configuracoes EF e DbSets de RBAC/refresh`

---

## Task 5: Repositórios e PermissionResolver (Infrastructure)

**Files:**
- Create: `Infrastructure/Data/Repositories/Auth/RoleRepository.cs`, `PermissionRepository.cs`, `RefreshTokenStore.cs`
- Create: `Infrastructure/Services/Auth/PermissionResolver.cs`
- Modify: `Infrastructure/Data/Repositories/Auth/UsuarioRepository.cs` (novos métodos da Task 2)
- Modify: `Infrastructure/InfrastructureServiceCollectionExtensions.cs` (registrar `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenStore`, `IRefreshTokenGenerator`, `IPermissionResolver`)
- Test: `tests/GatumsGourmet.UnitTests/...` (resolver testado com in-memory na Task 7; aqui só compila)

**Interfaces:**
- Consumes: interfaces da Task 2; `GatumsDbContext` (Task 4).
- Produces: implementações concretas. `PermissionResolver.ResolveAsync` retorna nomes das roles e os `Codigo` distintos das permissões via `UsuarioRoles → RolePermissions → Permissions`.

- [ ] **Step 1: Implementar os repositórios e o resolver** (leituras `AsNoTracking`, seguindo o padrão do repo).
- [ ] **Step 2: Registrar no DI** (Scoped).
- [ ] **Step 3: Compilar Infrastructure.** Run: `dotnet build src/GatumsGourmet.Infrastructure`. Expected: SUCCESS.
- [ ] **Step 4: Commit** `feat: repositorios RBAC/refresh e PermissionResolver`

---

## Task 6: Migration transacional + data migration + ajuste do DbSeeder

**Files:**
- Create: `Infrastructure/Migrations/<timestamp>_AuthRbacRefreshToken.cs` (gerado via `dotnet ef`, depois editado para seed + data migration)
- Modify: `Infrastructure/Data/DbSeeder.cs` (criar admin vinculado à role Admin via `UsuarioRole`, não via enum)
- Test: `tests/GatumsGourmet.IntegrationTests/Auth/MigrationSeedTests.cs`

**Interfaces:**
- Consumes: configs da Task 4.
- Produces: tabelas criadas + roles de sistema (Admin, Cliente, Gerente) + catálogo de 12 permissões + vínculos role→permissão (Admin=todas; Gerente=conforme spec §3; Cliente=nenhuma) semeados; coluna legada `Role` copiada para `UsuarioRoles` e então removida.

- [ ] **Step 1: Escrever teste de integração** `MigrationSeedTests.Apos_migrar_existem_3_roles_de_sistema_e_catalogo_completo`: sobe a `CustomWebApplicationFactory` (que migra), e assevera 3 roles (`Sistema=true` para Admin/Cliente), 12 permissões, Admin com todas, Gerente com o conjunto da spec, e o admin seed vinculado à role Admin.
- [ ] **Step 2: Rodar e ver falhar.** Run: `dotnet test tests/GatumsGourmet.IntegrationTests --filter MigrationSeedTests`
- [ ] **Step 3: Gerar a migration** (`dotnet ef migrations add AuthRbacRefreshToken -p src/GatumsGourmet.Infrastructure -s src/GatumsGourmet.Api`) e **editar o `Up`**: após `CreateTable`, inserir seed (roles, permissions, role_permissions) e a data migration (`INSERT INTO UsuarioRoles SELECT Id, <roleId> FROM Usuarios WHERE Role = '...'`; `UPDATE Usuarios SET Ativo = true`), e só então `DropColumn Role`. Envolver em bloco idempotente. Ajustar o `Down` (recria `Role`, copia role de maior privilégio, dropa tabelas). Ajustar `DbSeeder` para vincular admin via `UsuarioRole`.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: migration RBAC com seed e data migration do enum legado`

---

## Task 7: Login, Register, GetCurrentUser com novo esquema

**Files:**
- Modify: `Application/UseCases/V1/Auth/Login/LoginHandler.cs`, `LoginResponse.cs` (novo shape), `RegisterUser/RegisterUserHandler.cs`, `GetCurrentUser/GetCurrentUserHandler.cs`
- Modify: `Application/ApplicationServiceCollectionExtensions.cs` (registros que mudarem de assinatura)
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Auth/Login/LoginHandlerTests.cs` (ajustar)

**Interfaces:**
- Consumes: `IPermissionResolver`, `IRefreshTokenGenerator`, `IRefreshTokenStore`, `IJwtTokenGenerator` (novo), `IUsuarioRepository`.
- Produces: `LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, LoginUserDto User)`. Login rejeita usuário com `Ativo=false`. Register cria Cliente vinculado à role Cliente (via `SetRolesAsync` ou `UsuarioRole` no AddAsync).

- [ ] **Step 1: Ajustar/escrever testes.** `LoginHandlerTests`: (a) credenciais válidas → `AccessToken` e `RefreshToken` não vazios e perm resolvidas; (b) **usuário `Ativo=false` → `Fail`** (Review Focus); (c) senha errada → `Fail`. Mockar os novos serviços.
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar.** Login: valida senha, checa `Ativo`, resolve roles/permissões, gera access + refresh (grava hash no store com `FamiliaId=Guid.NewGuid()`), retorna novo `LoginResponse`. Register: vincula role Cliente. GetCurrentUser: retornar roles do usuário no lugar da antiga string role.
- [ ] **Step 4: Rodar e ver passar; compilar a solução inteira.** Run: `dotnet build GatumsGourmet.sln`. Expected: SUCCESS (fecha a quebra da Task 1).
- [ ] **Step 5: Commit** `feat: login com access+refresh e register vinculado a role`

---

## Task 8: RefreshToken, Logout, LogoutAll (use cases)

**Files:**
- Create: `Application/UseCases/V1/Auth/RefreshToken/` (Handler, Interface, Request, Response), `Logout/`, `LogoutAll/`
- Modify: `Application/ApplicationServiceCollectionExtensions.cs`
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Auth/RefreshToken/RefreshTokenHandlerTests.cs`

**Interfaces:**
- Consumes: `IRefreshTokenStore`, `IRefreshTokenGenerator`, `IJwtTokenGenerator`, `IPermissionResolver`, `IUsuarioRepository`, `IClock`.
- Produces: `RefreshTokenHandler.Handle(RefreshTokenRequest(string RefreshToken), string? ip, ct)` → `Result<LoginResponse>`. `LogoutHandler.Handle(Guid userId, string refreshToken, ct)`. `LogoutAllHandler.Handle(Guid userId, ct)`.

- [ ] **Step 1: Escrever testes** `RefreshTokenHandlerTests`: (a) refresh válido → rotaciona (token antigo revogado com `SubstituidoPor`, novo emitido, perm recalculada); (b) **refresh já revogado/substituído reapresentado → revoga família e `Fail`** (Review Focus); (c) **refresh expirado → `Fail`** (Review Focus); (d) **usuário `Ativo=false` → `Fail`** (Review Focus).
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** os três handlers conforme spec §4 (hash → busca → validações → detecção de reúso via flag revogado → rotação).
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: refresh rotativo com deteccao de reuso, logout e logout-all`

---

## Task 9: Endpoints de Auth (v1) atualizados

**Files:**
- Modify: `Api/Endpoints/V1/AuthEndpoints.cs` (adicionar `/refresh` público, `/logout` revoga, `/logout-all`; manter `/login`, `/register`, `/me`)
- Modify: `Api/Extensions/ClaimsPrincipalExtensions.cs` (`GetUserId` mantém; adicionar `HasPermission(this ClaimsPrincipal, string codigo)`)
- Test: `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Auth/` (login→refresh→reuso→logout)

**Interfaces:**
- Consumes: handlers das Tasks 7-8.
- Produces: rotas `POST /api/v1/auth/{login,register,refresh,logout,logout-all}`, `GET /api/v1/auth/me`.

- [ ] **Step 1: Escrever teste de integração** do fluxo: login → usa access em `/me` → `/refresh` rotaciona → reapresenta refresh antigo → 401 → `/logout`.
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** os endpoints (ler `X-Forwarded-For`/`RemoteIpAddress` para `ip` do refresh).
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: endpoints de auth com refresh/logout/logout-all`

---

## Task 10: Autorização por permissão dinâmica (Api)

**Files:**
- Create: `Api/Authorization/PermissionRequirement.cs`, `PermissionAuthorizationHandler.cs`, `PermissionPolicyProvider.cs`, `RequirePermissionExtensions.cs`
- Modify: `Api/Extensions/AuthenticationExtensions.cs` (remover policies `AdminOnly`/`ClienteOnly`; registrar `IAuthorizationPolicyProvider` → `PermissionPolicyProvider` e o handler)
- Modify: `Api/Program.cs` (se necessário para o provider)
- Test: `tests/GatumsGourmet.IntegrationTests/Auth/PermissionPolicyTests.cs`

**Interfaces:**
- Produces: `RequirePermission(this RouteHandlerBuilder, string codigo)`; policy name `perm:<codigo>`; handler aprova se claim `perm` contém `<codigo>`.

- [ ] **Step 1: Escrever teste de integração** `PermissionPolicyTests`: (a) token com `perm=produtos:escrever` acessa rota protegida (200/201); (b) **token sem a permissão → 403** (Review Focus, não 500).
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** requirement + handler + provider dinâmico (prefixo `perm:`) + extension.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: autorizacao por permissao via policy provider dinamico`

---

## Task 11: Aplicar permissões nas rotas existentes

**Files:**
- Modify: `Api/Endpoints/V1/ProdutosEndpoints.cs`, `CategoriasEndpoints.cs`, `ClientesEndpoints.cs`, `PedidosEndpoints.cs`, `PagamentosEndpoints.cs`, `RelatoriosEndpoints.cs`
- Modify: `Api/Extensions/ClaimsPrincipalExtensions.cs` (`IsAdmin()` → baseado em `HasPermission("pedidos:ler")` onde usado para "ver todos")
- Test: `tests/GatumsGourmet.IntegrationTests/...` (ajustar os EndpointTests que usavam AdminOnly/ClienteOnly)

**Interfaces:**
- Consumes: `RequirePermission` (Task 10).
- Produces: matriz da spec §3 aplicada (produtos/categorias:escrever, clientes:ler/escrever, pedidos:avancar, pagamentos:validar, relatorios:ler; carrinho/checkout/pagamento-próprio = `RequireAuthorization()` + ownership).

- [ ] **Step 1: Ajustar os testes de integração existentes** para autenticar com um usuário que tenha a permissão certa (Admin tem todas) e esperar 403 quando faltar.
- [ ] **Step 2: Rodar e ver falhar** (rotas ainda com policy antiga).
- [ ] **Step 3: Trocar** `RequireAuthorization("AdminOnly"/"ClienteOnly")` por `RequirePermission(...)` ou `RequireAuthorization()`+ownership conforme a matriz; ajustar `IsAdmin()`.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `refactor: proteger rotas por permissao em vez de role`

---

## Task 12: CRUD de Roles e Permissions (use cases + endpoints)

**Files:**
- Create: `Application/UseCases/V1/Roles/{CreateRole,ListRoles,GetRoleById,UpdateRole,DeleteRole,SetRolePermissions}/`
- Create: `Application/UseCases/V1/Permissions/{ListPermissions,GetPermissionById,CreatePermission,UpdatePermission,DeletePermission}/`
- Create: `Api/Endpoints/V1/RolesEndpoints.cs`, `PermissionsEndpoints.cs`
- Modify: `Application/ApplicationServiceCollectionExtensions.cs`, `Api/Extensions/EndpointRouteBuilderExtensions.cs`
- Test: `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Roles/`, `.../Permissions/`

**Interfaces:**
- Consumes: `IRoleRepository`, `IPermissionRepository`, `RequirePermission`.
- Produces: rotas `/api/v1/roles` (`roles:gerenciar`) e `/api/v1/permissions` (`permissoes:gerenciar`) conforme spec §6.

- [ ] **Step 1: Escrever testes de integração**: criar role, vincular permissões (`PUT /roles/{id}/permissions`), listar; **deletar role de sistema → bloqueado**; CRUD de permission.
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** handlers (1 por pasta) e endpoints. `DeleteRole` bloqueia `Sistema=true` ou `CountUsuariosAsync>0`.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: CRUD de roles e permissions via API`

---

## Task 13: Gestão de Usuários (use cases + endpoints) com regra "nunca sem admin"

**Files:**
- Create: `Application/UseCases/V1/Usuarios/{CreateUsuario,ListUsuarios,GetUsuarioById,SetUsuarioRoles,SetUsuarioAtivo,DeleteUsuario}/`
- Create: `Api/Endpoints/V1/UsuariosEndpoints.cs`
- Modify: `Application/ApplicationServiceCollectionExtensions.cs`, `Api/Extensions/EndpointRouteBuilderExtensions.cs`
- Test: `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Usuarios/`

**Interfaces:**
- Consumes: `IUsuarioRepository` (ampliado), `IRoleRepository`, `IPasswordHasher`, `RequirePermission`.
- Produces: rotas `/api/v1/usuarios` (`usuarios:gerenciar`) conforme spec §6; `DELETE` = soft-delete (`Ativo=false`).

- [ ] **Step 1: Escrever testes de integração**: criar usuário com role Gerente; `PUT /usuarios/{id}/roles`; `DELETE` faz soft-delete (usuário fica `Ativo=false`, não some); **remover a role Admin do último admin / desativar / excluir o último admin → bloqueado** (Review Focus), usando `CountAdminsAtivosAsync`.
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** handlers e endpoints com a regra "nunca sem admin" aplicada em SetUsuarioRoles, SetUsuarioAtivo e DeleteUsuario.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: gestao de usuarios com soft-delete e protecao do ultimo admin`

---

## Task 14: Housekeeping — PurgeExpiredRefreshTokens + BackgroundService + endpoint manual

**Files:**
- Create: `Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokens/` (Handler, Interface, Request, Response)
- Create: `Api/BackgroundServices/RefreshTokenCleanupService.cs`
- Modify: `Api/Endpoints/V1/AuthEndpoints.cs` (`POST /auth/maintenance/purge-tokens`, `usuarios:gerenciar`)
- Modify: `Application/ApplicationServiceCollectionExtensions.cs`, `Api/Program.cs` (registrar `AddHostedService`), `appsettings.json` (`Auth:RefreshTokenRetentionDays`, `Auth:CleanupIntervalHours`)
- Test: `tests/GatumsGourmet.UnitTests/Application/UseCases/V1/Manutencao/PurgeExpiredRefreshTokensHandlerTests.cs`

**Interfaces:**
- Consumes: `IRefreshTokenStore.DeleteExpiredAsync`, `IClock`.
- Produces: `PurgeExpiredRefreshTokensHandler.Handle(ct)` → `Result<int>` (qtd removida); `RefreshTokenCleanupService : BackgroundService` que roda em `CleanupIntervalHours` e chama o handler em escopo próprio.

- [ ] **Step 1: Escrever teste** `PurgeExpiredRefreshTokensHandlerTests`: tokens com `ExpiraEm < now` e revogados fora da retenção são removidos; tokens válidos permanecem. **Refresh expirado é elegível à purga** (Review Focus).
- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** handler (cutoff = `now - RetentionDays`), BackgroundService (loop com `PeriodicTimer`, escopo via `IServiceScopeFactory`), endpoint manual.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** `feat: housekeeping de refresh tokens (background + endpoint manual)`

---

## Task 15: Remover enum legado e DI final; build + suíte completa

**Files:**
- Delete: `Domain/Enums/Auth/UserRole.cs` (se nenhuma referência restar)
- Modify: qualquer `using`/registro remanescente
- Test: suíte inteira

- [ ] **Step 1: Procurar referências** a `UserRole` em todo `src`. Se só restar a definição do enum, remover o arquivo; senão, corrigir cada referência.
- [ ] **Step 2: Build da solução.** Run: `dotnet build GatumsGourmet.sln`. Expected: 0 erros.
- [ ] **Step 3: Rodar a suíte completa.** Run unit: `dotnet test tests/GatumsGourmet.UnitTests`. Run integração (container de teste de pé): `dotnet test tests/GatumsGourmet.IntegrationTests`. Expected: tudo verde.
- [ ] **Step 4: Commit** `chore: remover UserRole legado e fechar DI de auth`

---

## Task 16: Documentação (ADRs, scripts, guias, API-Reference)

**Files:**
- Create: `docs/adr/0001-rbac-com-permissoes.md`, `0002-refresh-token-rotativo-postgres.md`, `0003-autorizacao-por-permissao-dinamica.md`, `0004-limpeza-refresh-tokens.md`
- Create: `docs/sql/seed-roles-permissions.sql`
- Create: `docs/Auth/Guia-Permissoes.md`, `docs/Auth/Guia-de-Uso.md`
- Modify: `docs/Site/API-Reference.md` (seção de auth nova + matriz de rotas)

- [ ] **Step 1: Escrever as 4 ADRs** (Contexto/Decisão/Consequências/Status), o script SQL de seed (espelho da migration), o guia de permissões (passo a passo ponta a ponta) e o guia de uso (login/refresh/roles). Atualizar o API-Reference.
- [ ] **Step 2: Conferência** de que o script SQL reflete exatamente roles/permissões/vínculos da migration (Task 6).
- [ ] **Step 3: Commit** `docs: ADRs, scripts de seed e guias de autenticacao`

---

## Notas de execução

- A solução **não compila entre as Tasks 1 e 7** (remoção da propriedade `Role` do `Usuario`). Isso é intencional e documentado; o build só é exigido verde a partir da Task 7, Step 4. Execução deve seguir a ordem.
- Container de teste de integração (`gatums-gourmet-db-test-integration`, porta 5433) precisa estar de pé para as tasks com testes de integração (6, 9, 10, 11, 12, 13) e para a Task 15.
- DI manual: não esquecer de registrar cada handler/repo/service novo (checado no build).
