# Task 2: Interfaces de repositório e service (Domain)

**Projeto:** API .NET 10 (Clean Architecture) Gatums Gourmet, branch feature/AjusteLoginToken. Trabalhe em c:\Git\Botas\gatums-gourmet-api. Camada Domain não depende de nada.

**Objetivo:** criar/editar as interfaces (contratos) de repositório e service que as tasks seguintes vão implementar e consumir. São só interfaces — sem lógica, sem teste próprio; a validação é compilar o Domain.

## Constraints globais
- **Uma interface por arquivo.** Interfaces de repositório em `src/GatumsGourmet.Domain/Repositories/`; interfaces de service em `src/GatumsGourmet.Domain/Services/Interfaces/`.
- net10.0, nullable enable. Seguir o padrão das interfaces existentes (ver `IUsuarioRepository.cs`, `IJwtTokenGenerator.cs`).
- Entidades já existem (Task 1): `Role`, `Permission`, `RolePermission`, `UsuarioRole`, `RefreshToken`, `Usuario` em `GatumsGourmet.Domain.Entities.Auth`.

## Arquivos a CRIAR

`src/GatumsGourmet.Domain/Repositories/IRoleRepository.cs`:
```csharp
using GatumsGourmet.Domain.Entities.Auth;
namespace GatumsGourmet.Domain.Repositories;
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Role?> GetByNomeAsync(string nome, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    void Update(Role role);
    void Remove(Role role);
    Task SetPermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken ct = default);
    Task<int> CountUsuariosAsync(int roleId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

`src/GatumsGourmet.Domain/Repositories/IPermissionRepository.cs`:
```csharp
using GatumsGourmet.Domain.Entities.Auth;
namespace GatumsGourmet.Domain.Repositories;
public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Permission?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Permission permission, CancellationToken ct = default);
    void Update(Permission permission);
    void Remove(Permission permission);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

`src/GatumsGourmet.Domain/Repositories/IRefreshTokenStore.cs`:
```csharp
using GatumsGourmet.Domain.Entities.Auth;
namespace GatumsGourmet.Domain.Repositories;
public interface IRefreshTokenStore
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeFamilyAsync(Guid familiaId, string motivo, DateTime em, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid usuarioId, string motivo, DateTime em, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

`src/GatumsGourmet.Domain/Services/Interfaces/IRefreshTokenGenerator.cs`:
```csharp
namespace GatumsGourmet.Domain.Services.Interfaces;
public interface IRefreshTokenGenerator
{
    (string PlainToken, string Hash) Generate();
    string Hash(string plainToken);
}
```

`src/GatumsGourmet.Domain/Services/Interfaces/IPermissionResolver.cs`:
```csharp
namespace GatumsGourmet.Domain.Services.Interfaces;
public interface IPermissionResolver
{
    Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissoes)> ResolveAsync(Guid usuarioId, CancellationToken ct = default);
}
```

## Arquivos a MODIFICAR

`src/GatumsGourmet.Domain/Services/Interfaces/IJwtTokenGenerator.cs` — trocar a assinatura:
```csharp
namespace GatumsGourmet.Domain.Services.Interfaces;
public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissoes);
}
```
(Isso quebra a chamada atual em LoginHandler e a impl JwtTokenGenerator — intencional, corrigido em tasks 3 e 7. Não corrija aqui.)

`src/GatumsGourmet.Domain/Repositories/IUsuarioRepository.cs` — AMPLIAR (manter os métodos atuais `GetByEmailAsync`, `GetByIdAsync`, `AddAsync`, `SaveChangesAsync` e adicionar):
```csharp
    Task<IReadOnlyList<Usuario>> ListAsync(CancellationToken ct = default);
    Task<Usuario?> GetByIdComRolesAsync(Guid id, CancellationToken ct = default);
    Task SetRolesAsync(Guid usuarioId, IEnumerable<int> roleIds, CancellationToken ct = default);
    Task SetAtivoAsync(Guid usuarioId, bool ativo, CancellationToken ct = default);
    Task<int> CountAdminsAtivosAsync(CancellationToken ct = default);
```

## Validação
Rode `dotnet build src/GatumsGourmet.Domain`. Deve compilar com 0 erros (o Domain não referencia implementações). NÃO rode build da solução inteira (Application quebrada de propósito desde a Task 1).

## Commit
`feat: interfaces de repositorio e service para RBAC/refresh`

## Não dispare subagentes. Não corrija consumidores quebrados (intencional). Não rode build da solução inteira.
