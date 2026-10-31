# Task 13: Gestão de Usuários (U2) com soft-delete e regra "nunca sem admin"

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 127 unit + 90 integração verdes.

**Objetivo:** gestão de usuários administrativos via API — criar usuário com roles, listar, detalhar, definir roles, ativar/desativar, soft-delete. Com a regra de segurança: NUNCA deixar o sistema sem um admin ativo.

## Constraints globais
- net10.0, noBOM. Use cases em `Application/UseCases/V1/Usuarios/<UseCase>/`. Endpoints em `Api/Endpoints/V1/UsuariosEndpoints.cs`. Padrão do repo (Handler + Request + Response + interface no mesmo arquivo).
- Todas as rotas com `RequirePermission("usuarios:gerenciar")`.
- Repositórios (Task 5): `IUsuarioRepository { GetByEmailAsync, GetByIdAsync, GetByIdComRolesAsync, ListAsync, AddAsync, SetRolesAsync, SetAtivoAsync, CountAdminsAtivosAsync, SaveChangesAsync }`, `IRoleRepository { GetByIdAsync, GetByNomeAsync, ListAsync, ... }`, `IPasswordHasher`, `IClock`.
- Entidade `Usuario { Id, Nome, Email, Telefone?, SenhaHash, Ativo, CriadoEm, UsuarioRoles }`; `UsuarioRole { UsuarioId, RoleId, Role }`.

## Regra crítica "nunca sem admin" (REVIEW FOCUS)
Antes de qualquer operação que possa remover o último admin ativo, checar via `CountAdminsAtivosAsync` + o estado do alvo. Bloquear com Fail "O sistema deve ter ao menos um administrador ativo." quando:
- **SetUsuarioRoles**: o usuário alvo é admin ativo E as novas roles NÃO incluem a role Admin E ele é o único admin ativo (`CountAdminsAtivosAsync == 1`).
- **SetUsuarioAtivo(false)**: o alvo é admin ativo E é o único admin ativo.
- **DeleteUsuario** (soft-delete → Ativo=false): mesma regra do SetUsuarioAtivo(false).
Para saber se o alvo "é admin", carregue com GetByIdComRolesAsync e cheque se tem vínculo com role de nome "Admin".

## Use cases (`Application/UseCases/V1/Usuarios/`)

Shape compartilhado: `UsuarioResponse(Guid Id, string Nome, string Email, string? Telefone, bool Ativo, IReadOnlyList<string> Roles, DateTime CriadoEm)` em `Usuarios/UsuarioResponse.cs`.

- **ListUsuarios/** `IListUsuariosHandler.Handle(ct) → Result<IReadOnlyList<UsuarioResponse>>` — lista todos (via ListAsync, com roles). (Sem paginação para simplificar; pode retornar lista. Se preferir paginar seguindo o padrão, use PaginatedResponse — mas o brief aceita lista simples aqui.)
- **GetUsuarioById/** `Handle(Guid id) → Result<UsuarioResponse>` — GetByIdComRolesAsync; Fail "Usuário não encontrado." se null.
- **CreateUsuario/** `CreateUsuarioRequest(string Nome, string Email, string Senha, string? Telefone, IReadOnlyList<int> RoleIds)`:
  - Fail se email já existe (GetByEmailAsync).
  - Cria `Usuario { Id=Guid.NewGuid(), Nome, Email, Telefone, SenhaHash=hasher.Hash(Senha), Ativo=true, CriadoEm=clock.UtcNow }`; AddAsync + SaveChanges.
  - Vincula roles: `SetRolesAsync(usuario.Id, RoleIds, ct)` + SaveChanges. (Valida que os RoleIds existem? Opcional — se um id não existir, o FK falha; melhor validar com IRoleRepository.GetByIdAsync para cada, ou deixar o banco rejeitar. Decida e registre; validar é mais amigável.)
  - Retorna `CreateUsuarioResponse(Guid Id)`.
- **SetUsuarioRoles/** `SetUsuarioRolesRequest(Guid UsuarioId, IReadOnlyList<int> RoleIds)`:
  - GetByIdComRolesAsync; Fail se null.
  - Aplica a REGRA "nunca sem admin" (ver acima).
  - `SetRolesAsync(id, RoleIds, ct)` + SaveChanges; Ok(true).
- **SetUsuarioAtivo/** `SetUsuarioAtivoRequest(Guid UsuarioId, bool Ativo)`:
  - GetByIdComRolesAsync; Fail se null.
  - Se Ativo==false, aplica a REGRA "nunca sem admin".
  - `SetAtivoAsync(id, Ativo, ct)` + SaveChanges; Ok(true).
- **DeleteUsuario/** `Handle(Guid id) → Result<bool>` — SOFT-DELETE:
  - GetByIdComRolesAsync; Fail se null.
  - Aplica a REGRA "nunca sem admin" (equivale a desativar).
  - `SetAtivoAsync(id, false, ct)` + SaveChanges; Ok(true). (NÃO remove a linha; preserva histórico de pedidos.)

## Endpoints `UsuariosEndpoints.cs` — todos `RequirePermission("usuarios:gerenciar")`
- GET `/` → ListUsuarios
- GET `/{id:guid}` → GetUsuarioById
- POST `/` → CreateUsuario (201)
- PUT `/{id:guid}/roles` → SetUsuarioRoles (`request with { UsuarioId = id }`)
- PATCH `/{id:guid}/ativo` → SetUsuarioAtivo (body com Ativo; `request with { UsuarioId = id }`)
- DELETE `/{id:guid}` → DeleteUsuario

## DI + roteamento
Registrar os 6 handlers; mapear `UsuariosEndpoints.Map(v1.MapGroup("/usuarios").WithTags("Usuarios"));`.

## Testes de integração (`tests/.../V1/Usuarios/`)
Admin autenticado (tem usuarios:gerenciar). GET /roles ou GET /permissions não precisa; para roles use GET /api/v1/roles para pegar ids (ex.: id da role Gerente e Admin).
Casos:
1. Criar usuário com role Gerente → 201; GET confirma Ativo=true e roles contém "Gerente".
2. PUT /{id}/roles trocando para outra role → 200; GET confirma.
3. PATCH /{id}/ativo false → 200; GET confirma Ativo=false.
4. DELETE /{id} → 200 (soft-delete); GET /{id} ainda retorna o usuário com Ativo=false (não some).
5. **REVIEW FOCUS — último admin:** o seed garante 1 admin (admin@gatums.com). Tente desativar/deletar o último admin ativo → 400 Fail. (Cuidado: se CreateAdminClientAsync cria admins extras de teste, pode haver mais de 1 admin ativo e a regra não dispararia. Para testar de forma determinística: crie um cenário controlado — ex.: crie um usuário admin novo e único num teste que primeiro conta os admins; OU teste a regra via o handler unitariamente com CountAdminsAtivosAsync mockado retornando 1. PREFIRA um teste UNITÁRIO do SetUsuarioAtivoHandler/DeleteUsuarioHandler com mocks para a regra "nunca sem admin" ser determinística, e um teste de integração do happy path. Decida e registre.)
6. Cliente (sem usuarios:gerenciar) em qualquer rota /usuarios → 403.

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test` unit + integração verdes (reporte contagens). Investigue regressões.

## Commit
`feat: gestao de usuarios com soft-delete e protecao do ultimo admin`

## Não dispare subagentes. A regra "nunca sem admin" é o ponto crítico — garanta cobertura de teste determinística (unitária com mock de CountAdminsAtivosAsync é a forma mais segura).
