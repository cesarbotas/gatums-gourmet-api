# Task 5: Repositórios RBAC/refresh + PermissionResolver + UsuarioRepository ampliado + DI

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Camada Infrastructure, EF Core + Npgsql.

**Objetivo:** implementar os repositórios concretos (`RoleRepository`, `PermissionRepository`, `RefreshTokenStore`), o `PermissionResolver`, ampliar o `UsuarioRepository`, e registrar tudo no DI.

## Constraints globais
- **Uma classe por arquivo.** Repositórios em `src/GatumsGourmet.Infrastructure/Data/Repositories/Auth/`; o PermissionResolver em `src/GatumsGourmet.Infrastructure/Services/Auth/PermissionResolver.cs`.
- Seguir o padrão dos repositórios existentes: construtor primário `(GatumsDbContext db)`, leituras com `AsNoTracking()`. Ver `UsuarioRepository.cs` e `CategoriaRepository.cs` (padrão abaixo).
- net10.0, noBOM, ferramentas de escrita.
- Interfaces (Task 2) e entidades (Task 1) e DbSets (Task 4) já existem. DbSets: `db.Roles`, `db.Permissions`, `db.RolePermissions`, `db.UsuarioRoles`, `db.RefreshTokens`.

## Padrão (UsuarioRepository atual)
```csharp
public class UsuarioRepository(GatumsDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
    public async Task AddAsync(Usuario usuario, CancellationToken ct = default) => await db.Usuarios.AddAsync(usuario, ct);
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

## Trabalho

### 1. `Data/Repositories/Auth/RoleRepository.cs` : IRoleRepository
Implementar conforme a interface (Task 2):
- `GetByIdAsync(int id)` — inclui as permissões: `db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission).FirstOrDefaultAsync(r => r.Id == id, ct)` (NÃO AsNoTracking em Get que será usado para Update; use AsNoTracking só em ListAsync/GetByNome de leitura pura).
- `GetByNomeAsync(string nome)` — AsNoTracking, FirstOrDefault por Nome.
- `ListAsync()` — AsNoTracking, Include permissões, OrderBy Nome, ToList.
- `AddAsync(Role)` — db.Roles.AddAsync.
- `Update(Role)` — db.Roles.Update(role).
- `Remove(Role)` — db.Roles.Remove(role).
- `SetPermissionsAsync(int roleId, IEnumerable<int> permissionIds, ct)` — remove os RolePermissions atuais da role (`db.RolePermissions.Where(rp => rp.RoleId == roleId)`) e adiciona os novos (`permissionIds.Select(pid => new RolePermission { RoleId = roleId, PermissionId = pid })`). NÃO chamar SaveChanges aqui (o handler chama).
- `CountUsuariosAsync(int roleId)` — `db.UsuarioRoles.CountAsync(ur => ur.RoleId == roleId, ct)`.
- `SaveChangesAsync()`.

### 2. `Data/Repositories/Auth/PermissionRepository.cs` : IPermissionRepository
CRUD direto sobre `db.Permissions` (GetById, GetByCodigo AsNoTracking, ListAsync AsNoTracking OrderBy Codigo, AddAsync, Update, Remove, SaveChangesAsync).

### 3. `Data/Repositories/Auth/RefreshTokenStore.cs` : IRefreshTokenStore
- `AddAsync(RefreshToken)` — db.RefreshTokens.AddAsync.
- `GetByHashAsync(string tokenHash)` — SEM AsNoTracking (será atualizado na rotação): `db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)`.
- `RevokeFamilyAsync(Guid familiaId, string motivo, DateTime em, ct)` — carrega todos os tokens não revogados da família e seta `Revogado=true, RevogadoEm=em, MotivoRevogacao=motivo`. Não dar SaveChanges? Dê SaveChanges aqui (operação atômica de revogação). Use `ExecuteUpdateAsync` se preferir, mas a versão com carregar+setar+SaveChanges é aceitável.
- `RevokeAllForUserAsync(Guid usuarioId, string motivo, DateTime em, ct)` — idem para todos os tokens não revogados do usuário; SaveChanges.
- `DeleteExpiredAsync(DateTime cutoff, ct)` — remove tokens onde `(ExpiraEm < cutoff) OR (Revogado && RevogadoEm < cutoff)`; retorna a contagem removida (`ExecuteDeleteAsync` retorna int). SaveChanges não necessário com ExecuteDeleteAsync.
- `SaveChangesAsync()`.

### 4. `Services/Auth/PermissionResolver.cs` : IPermissionResolver (namespace `...Services.Auth`)
```
ResolveAsync(Guid usuarioId, ct):
  - roles = db.UsuarioRoles.Where(ur => ur.UsuarioId == usuarioId).Select(ur => ur.Role.Nome).Distinct().ToList (AsNoTracking)
  - permissoes = db.UsuarioRoles.Where(ur => ur.UsuarioId == usuarioId)
       .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Codigo)).Distinct().ToList
  - retorna (roles, permissoes) como IReadOnlyList<string>
```
Use o DbContext diretamente (injetado). AsNoTracking. Faça em 1 ou 2 queries eficientes.

### 5. Ampliar `Data/Repositories/Auth/UsuarioRepository.cs`
Manter os 4 métodos atuais e adicionar:
- `ListAsync()` — AsNoTracking, Include UsuarioRoles.Role, OrderBy Nome.
- `GetByIdComRolesAsync(Guid id)` — Include UsuarioRoles.Role (SEM AsNoTracking, usado para edição de roles).
- `SetRolesAsync(Guid usuarioId, IEnumerable<int> roleIds, ct)` — remove os UsuarioRoles atuais do usuário e adiciona os novos; sem SaveChanges (handler chama).
- `SetAtivoAsync(Guid usuarioId, bool ativo, ct)` — carrega o usuário (tracked), seta Ativo; sem SaveChanges.
- `CountAdminsAtivosAsync(ct)` — conta usuários Ativos que têm a role de nome "Admin": `db.UsuarioRoles.CountAsync(ur => ur.Role.Nome == "Admin" && ur.Usuario.Ativo, ct)`.

### 6. Registrar no DI (`InfrastructureServiceCollectionExtensions.cs`)
Adicionar (Scoped): `IRoleRepository→RoleRepository`, `IPermissionRepository→PermissionRepository`, `IRefreshTokenStore→RefreshTokenStore`, `IPermissionResolver→PermissionResolver`. (O `IRefreshTokenGenerator` já foi registrado na Task 3.) Ajustar usings se necessário.

## Validação
`dotnet build src/GatumsGourmet.Infrastructure`. A Application continua quebrada (Task 7) — confirme que não há erros em arquivos de Infrastructure. Reporte como concern. NÃO corrija a Application.

## Commit
`feat: repositorios RBAC/refresh e PermissionResolver`

## Não dispare subagentes. Não corrija a Application. Não chame SaveChanges em SetPermissions/SetRoles (os handlers fazem).
