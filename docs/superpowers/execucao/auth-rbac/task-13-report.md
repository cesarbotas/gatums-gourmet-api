# Task 13 — Gestão de Usuários (U2) com soft-delete e proteção do último admin

## Resumo
Implementada a gestão de usuários administrativos via API (criar com roles,
listar, detalhar, definir roles, ativar/desativar, soft-delete), aplicando a
regra crítica de segurança "nunca deixar o sistema sem admin ativo". Build 0
erros; 140 testes unitários + 95 de integração verdes.

## Use cases (`src/GatumsGourmet.Application/UseCases/V1/Usuarios/`)
Shape compartilhado `UsuarioResponse(Id, Nome, Email, Telefone?, Ativo, Roles, CriadoEm)`
em `UsuarioResponse.cs`. Um helper interno `UsuarioMapper` centraliza o mapeamento
entidade→DTO, a constante `AdminRoleNome = "Admin"` e `TemRoleAdmin(usuario)`.

- **ListUsuarios** — `IListUsuariosHandler.Handle(ct) → Result<IReadOnlyList<UsuarioResponse>>`.
  Lista todos via `ListAsync` (já traz UsuarioRoles.Role). Sem paginação (lista simples, conforme brief).
- **GetUsuarioById** — `Handle(Guid id)` via `GetByIdComRolesAsync`; Fail "Usuário não encontrado." se null.
- **CreateUsuario** — `CreateUsuarioRequest(Nome, Email, Senha, Telefone?, RoleIds)` + validator FluentValidation.
  Fail se e-mail já existe; **valida que cada RoleId existe** (`IRoleRepository.GetByIdAsync`) antes de persistir
  (decisão: validação amigável em vez de deixar o FK estourar); cria com `SenhaHash=hasher.Hash`, `Ativo=true`,
  `CriadoEm=clock.UtcNow`; `AddAsync`+Save, depois `SetRolesAsync`+Save. Retorna `CreateUsuarioResponse(Id)`.
- **SetUsuarioRoles** — `SetUsuarioRolesRequest(UsuarioId, RoleIds)`; valida existência dos RoleIds; aplica a regra; `SetRolesAsync`+Save.
- **SetUsuarioAtivo** — `SetUsuarioAtivoRequest(UsuarioId, Ativo)`; se `Ativo==false` aplica a regra; `SetAtivoAsync`+Save.
- **DeleteUsuario** — `Handle(Guid id)` SOFT-DELETE: aplica a regra e faz `SetAtivoAsync(id, false)`+Save. **Nunca remove a linha** (preserva histórico de pedidos).

## Regra "nunca sem admin" — como foi implementada
Centralizada na checagem `usuario.Ativo && UsuarioMapper.TemRoleAdmin(usuario) && CountAdminsAtivosAsync() <= 1`.
Carrega o alvo com `GetByIdComRolesAsync` para saber se tem vínculo com a role "Admin".
- **SetUsuarioRoles:** só bloqueia se o alvo é admin ativo E as novas roles NÃO incluem a role Admin
  (resolvida por `IRoleRepository.GetByNomeAsync("Admin")` + `RoleIds.Contains(adminRole.Id)`) E `CountAdminsAtivosAsync() <= 1`.
- **SetUsuarioAtivo(false) / DeleteUsuario:** bloqueia quando o alvo é admin ativo e é o único admin ativo.
Mensagem única de Fail: **"O sistema deve ter ao menos um administrador ativo."** (→ 400 via `ToHttpResult`).
Usei `<= 1` (em vez de `== 1`) por robustez defensiva.

## Como a regra foi testada (determinística)
`tests/.../UnitTests/.../V1/Usuarios/UltimoAdminRuleTests.cs` — testes UNITÁRIOS com
`CountAdminsAtivosAsync` **mockado**, tornando a regra independente do estado do banco
(que pode ter vários admins por causa das factories). Cobre:
- SetRoles remove Admin do único admin ativo (count=1) → bloqueia; mantém Admin → permite; há outro admin (count=2) → permite.
- SetAtivo(false) no único admin → bloqueia; com outro admin → permite; ativar (true) → não aplica a regra.
- Delete do único admin → bloqueia; com outro admin → soft-delete; usuário não-admin → soft-delete sem checar; inexistente → Fail.
`CreateUsuarioHandlerTests.cs` cobre happy path (hash+roles), e-mail duplicado e RoleId inexistente.

## Endpoints (`src/GatumsGourmet.Api/Endpoints/V1/UsuariosEndpoints.cs`)
Todos com `RequirePermission("usuarios:gerenciar")`:
GET `/`, GET `/{id:guid}`, POST `/` (201), PUT `/{id:guid}/roles` (`request with { UsuarioId = id }`),
PATCH `/{id:guid}/ativo` (`request with { UsuarioId = id }`), DELETE `/{id:guid}`.
Mapeado em `EndpointRouteBuilderExtensions`: `UsuariosEndpoints.Map(v1.MapGroup("/usuarios").WithTags("Usuarios"))`.

## DI
Registrados os 6 handlers em `ApplicationServiceCollectionExtensions` (`AddScoped`). O validator é descoberto automaticamente por `AddValidatorsFromAssemblyContaining`.

## Integração (happy path) — `tests/.../IntegrationTests/.../V1/Usuarios/UsuariosEndpointTests.cs`
Admin autenticado (tem `usuarios:gerenciar`); IDs de role obtidos via GET /api/v1/roles.
1. Criar usuário com role Gerente → 201; GET confirma Ativo=true e roles contém "Gerente".
2. PUT /roles trocando Gerente→Cliente → 200; GET confirma.
3. PATCH /ativo false → 200; GET confirma Ativo=false.
4. DELETE → 200 soft-delete; GET ainda retorna o usuário com Ativo=false (não some).
5. Cliente (sem a permissão) em GET /usuarios → 403.

## Validação
- `dotnet build GatumsGourmet.sln` → **0 erros** (14 avisos pré-existentes: CS0105 em testes legados e MSB3277 de versão de EF; não introduzidos aqui).
- Unit: **140 aprovados / 0 falhas** (eram 127; +13 novos).
- Integração: **95 aprovados / 0 falhas** (eram 90; +5 novos). Container Postgres de teste (5433) de pé.
- Sem regressões.

## Arquivos alterados/criados
Novos (src): UsuarioResponse.cs, UsuarioMapper.cs, ListUsuarios/, GetUsuarioById/, CreateUsuario/, SetUsuarioRoles/, SetUsuarioAtivo/, DeleteUsuario/ handlers; Api/Endpoints/V1/UsuariosEndpoints.cs.
Modificados (src): Api/Extensions/EndpointRouteBuilderExtensions.cs; Application/ApplicationServiceCollectionExtensions.cs.
Novos (tests): UnitTests/.../V1/Usuarios/{UltimoAdminRuleTests,CreateUsuarioHandlerTests}.cs; IntegrationTests/.../V1/Usuarios/UsuariosEndpointTests.cs.

## Auto-revisão
- Padrão do repo seguido (Handler+Request+Response+interface no mesmo arquivo; `Result<T>`/`ToHttpResult`; `RequirePermission`).
- noBOM confirmado em todos os novos .cs.
- Regra "nunca sem admin" verificada ANTES de qualquer escrita em todos os 3 caminhos (set-roles, set-ativo, delete).
- Soft-delete nunca remove linha; usa `SetAtivoAsync(false)`.

## Concerns / decisões
- A regra "nunca sem admin" é intencionalmente coberta só por unit tests (determinística). O brief
  aceita e recomenda essa decisão; um teste de integração dependeria do número de admins no banco.
- `CreateUsuario` valida RoleIds com uma query por id (loop). Para payloads pequenos é aceitável;
  se no futuro houver muitas roles por request, vale um batch (`ListAsync`/`WHERE Id IN`).
- A resolução da role "Admin" por nome fixo ("Admin") é consistente com o seed e com `CountAdminsAtivosAsync`
  (que também filtra por `Role.Nome == "Admin"`).
