# Task 5 — Report: Repositórios RBAC/refresh + PermissionResolver + UsuarioRepository ampliado + DI

## O que foi implementado

### Novos arquivos (uma classe por arquivo)
1. **`Data/Repositories/Auth/RoleRepository.cs`** (`IRoleRepository`)
   - `GetByIdAsync` — Include RolePermissions→Permission, **tracked** (usado p/ Update).
   - `GetByNomeAsync` — AsNoTracking.
   - `ListAsync` — AsNoTracking + Include permissões + OrderBy Nome.
   - `AddAsync`, `Update`, `Remove`.
   - `SetPermissionsAsync` — remove os RolePermissions atuais (RemoveRange) e adiciona os novos (AddRangeAsync). **Sem SaveChanges** (handler faz).
   - `CountUsuariosAsync` — CountAsync em UsuarioRoles.
   - `SaveChangesAsync`.

2. **`Data/Repositories/Auth/PermissionRepository.cs`** (`IPermissionRepository`)
   - CRUD sobre `db.Permissions`. `GetByIdAsync` tracked; `GetByCodigoAsync`/`ListAsync` AsNoTracking (OrderBy Codigo).

3. **`Data/Repositories/Auth/RefreshTokenStore.cs`** (`IRefreshTokenStore`)
   - `AddAsync`.
   - `GetByHashAsync` — **sem** AsNoTracking (rotação atualiza a entidade).
   - `RevokeFamilyAsync` / `RevokeAllForUserAsync` — carregam tokens não revogados, setam Revogado/RevogadoEm/MotivoRevogacao e **dão SaveChanges** (operação atômica, conforme brief).
   - `DeleteExpiredAsync` — `ExecuteDeleteAsync` com `(ExpiraEm < cutoff) || (Revogado && RevogadoEm < cutoff)`; retorna a contagem removida.
   - `SaveChangesAsync`.

4. **`Services/Auth/PermissionResolver.cs`** (`IPermissionResolver`, namespace `...Services.Auth`)
   - Injeta `GatumsDbContext`. Duas queries AsNoTracking: roles (Distinct dos nomes) e permissões (SelectMany dos códigos, Distinct). Retorna tupla `(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissoes)`.

### Arquivo ampliado
5. **`Data/Repositories/Auth/UsuarioRepository.cs`** — mantidos os 4 métodos originais + 5 novos:
   - `ListAsync` — AsNoTracking + Include UsuarioRoles→Role + OrderBy Nome.
   - `GetByIdComRolesAsync` — Include UsuarioRoles→Role, **tracked** (edição de roles).
   - `SetRolesAsync` — remove UsuarioRoles atuais + adiciona novos. **Sem SaveChanges**.
   - `SetAtivoAsync` — carrega o usuário (tracked), seta Ativo. **Sem SaveChanges**.
   - `CountAdminsAtivosAsync` — `UsuarioRoles.CountAsync(ur => ur.Role.Nome == "Admin" && ur.Usuario.Ativo)`.

### DI (`InfrastructureServiceCollectionExtensions.cs`)
- Adicionados (Scoped): `IRoleRepository→RoleRepository`, `IPermissionRepository→PermissionRepository`, `IRefreshTokenStore→RefreshTokenStore`, `IPermissionResolver→PermissionResolver`.
- `IRefreshTokenGenerator` já estava registrado (Task 3). Usings já cobriam os novos tipos (`Data.Repositories.Auth`, `Services.Auth`, `Domain.Repositories`, `Domain.Services.Interfaces`).

## Resultado do build de Infrastructure
`dotnet build src/GatumsGourmet.Infrastructure` → **Exit 1**, porém **0 erros em arquivos de Infrastructure**. Todos os erros são de `GatumsGourmet.Application` (quebra proposital da Task 7): `Usuario.Role` em `LoginHandler.cs`, `RegisterUserHandler.cs`, `GetCurrentUserHandler.cs`. Como Infrastructure referencia Application (ProjectReference), o build transitivo compila Application e expõe esses erros. Nenhum erro em caminho `Infrastructure/`. Application NÃO foi alterada.

## Arquivos alterados/criados
- CRIADO `src/GatumsGourmet.Infrastructure/Data/Repositories/Auth/RoleRepository.cs`
- CRIADO `src/GatumsGourmet.Infrastructure/Data/Repositories/Auth/PermissionRepository.cs`
- CRIADO `src/GatumsGourmet.Infrastructure/Data/Repositories/Auth/RefreshTokenStore.cs`
- CRIADO `src/GatumsGourmet.Infrastructure/Services/Auth/PermissionResolver.cs`
- ALTERADO `src/GatumsGourmet.Infrastructure/Data/Repositories/Auth/UsuarioRepository.cs`
- ALTERADO `src/GatumsGourmet.Infrastructure/InfrastructureServiceCollectionExtensions.cs`

## Auto-revisão
- **AsNoTracking**: aplicado em leituras puras (`GetByNome`, `GetByCodigo`, todos os `ListAsync`, ambas as queries do PermissionResolver). Omitido onde a entidade será atualizada (`RoleRepository.GetByIdAsync`, `PermissionRepository.GetByIdAsync`, `UsuarioRepository.GetByIdComRolesAsync`, `SetAtivoAsync`, `RefreshTokenStore.GetByHashAsync`).
- **SaveChanges**: ausente em `SetPermissionsAsync`/`SetRolesAsync`/`SetAtivoAsync` (handlers fazem). Presente em `RevokeFamilyAsync`/`RevokeAllForUserAsync` (conforme brief). `DeleteExpiredAsync` usa `ExecuteDeleteAsync` (não precisa SaveChanges).
- **Queries eficientes**: Includes só onde necessário; `ExecuteDeleteAsync` em lote; `CountAsync` server-side; `Distinct` nas resoluções.
- **Uma classe por arquivo**: respeitado.
- **Padrão do repo**: construtor primário `(GatumsDbContext db)`, assinaturas idênticas às interfaces.
- Removido `using GatumsGourmet.Domain.Services.Interfaces;` não utilizado do UsuarioRepository (estava sem uso no original).

## Concerns
- Build da solução/Application continua quebrado DE PROPÓSITO (Task 7): `Usuario.Role` nos handlers de Auth. Esperado; não corrigido.
