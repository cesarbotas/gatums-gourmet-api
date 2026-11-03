# Task 1: Entidades de domínio do RBAC e refresh token

**Projeto:** API .NET 10 (Clean Architecture) Gatums Gourmet. Camada Domain não tem dependências de projeto. Arquivos `.cs` usam BOM UTF-8; preserve acentuação.

**Objetivo:** criar as 5 entidades novas do RBAC/refresh token e modificar a entidade `Usuario` para o novo modelo (sem o enum de role).

## Constraints globais (do plano/SDD)
- **Uma classe por arquivo**, agrupadas por pasta. Entidades de auth em `src/GatumsGourmet.Domain/Entities/Auth/`.
- Seguir o padrão do repo (ver `Usuario.cs` atual e outras entidades em `Entities/`).
- net10.0, nullable enable, ImplicitUsings enable (já configurado no `.csproj` do Domain).

## Arquivos

**Criar** (1 classe por arquivo) em `src/GatumsGourmet.Domain/Entities/Auth/`:

- `Role.cs`:
  ```
  public class Role
  {
      public int Id { get; set; }
      public string Nome { get; set; } = default!;
      public string? Descricao { get; set; }
      public bool Sistema { get; set; }
      public ICollection<RolePermission> RolePermissions { get; set; } = [];
      public ICollection<UsuarioRole> UsuarioRoles { get; set; } = [];
  }
  ```
- `Permission.cs`:
  ```
  public class Permission
  {
      public int Id { get; set; }
      public string Codigo { get; set; } = default!;   // formato recurso:acao
      public string? Descricao { get; set; }
      public string Recurso { get; set; } = default!;
      public string Acao { get; set; } = default!;
      public ICollection<RolePermission> RolePermissions { get; set; } = [];
  }
  ```
- `RolePermission.cs` (junção):
  ```
  public class RolePermission
  {
      public int RoleId { get; set; }
      public Role Role { get; set; } = default!;
      public int PermissionId { get; set; }
      public Permission Permission { get; set; } = default!;
  }
  ```
- `UsuarioRole.cs` (junção):
  ```
  public class UsuarioRole
  {
      public Guid UsuarioId { get; set; }
      public Usuario Usuario { get; set; } = default!;
      public int RoleId { get; set; }
      public Role Role { get; set; } = default!;
  }
  ```
- `RefreshToken.cs`:
  ```
  public class RefreshToken
  {
      public Guid Id { get; set; }
      public Guid UsuarioId { get; set; }
      public string TokenHash { get; set; } = default!;
      public Guid FamiliaId { get; set; }
      public DateTime ExpiraEm { get; set; }
      public DateTime CriadoEm { get; set; }
      public bool Revogado { get; set; }
      public DateTime? RevogadoEm { get; set; }
      public string? SubstituidoPorTokenHash { get; set; }
      public string? MotivoRevogacao { get; set; }
      public string? CriadoPorIp { get; set; }
  }
  ```

**Modificar** `src/GatumsGourmet.Domain/Entities/Auth/Usuario.cs`:
- Remover a propriedade `public UserRole Role { get; set; }`.
- Remover o `using GatumsGourmet.Domain.Enums.Auth;` (não precisa mais do enum).
- Adicionar `public bool Ativo { get; set; }`.
- Adicionar `public ICollection<UsuarioRole> UsuarioRoles { get; set; } = [];`.
- Manter o resto (`Id`, `Nome`, `Email`, `Telefone`, `SenhaHash`, `CriadoEm`, construtor vazio).

Estado atual de `Usuario.cs` para referência:
```csharp
using GatumsGourmet.Domain.Enums.Auth;

namespace GatumsGourmet.Domain.Entities.Auth;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Telefone { get; set; }
    public string SenhaHash { get; set; } = default!;
    public UserRole Role { get; set; }
    public DateTime CriadoEm { get; set; }

    public Usuario() { }
}
```

## Teste (TDD)

**Criar** `tests/GatumsGourmet.UnitTests/Domain/Entities/Auth/RefreshTokenTests.cs`:
- Teste `Novo_refresh_token_nao_nasce_revogado`: instancia `new RefreshToken()` e assevera `Revogado == false` e `RevogadoEm == null` (FluentAssertions, padrão do projeto). Fixa a invariante default usada na detecção de reúso.

Passos TDD:
1. Escrever o teste.
2. Rodar e ver falhar (tipo `RefreshToken` não existe): `dotnet test tests/GatumsGourmet.UnitTests --filter RefreshTokenTests`
3. Criar as 5 entidades + modificar `Usuario`.
4. Rodar e ver passar.

## IMPORTANTE — quebra de build intencional
Remover a propriedade `Role` de `Usuario` **quebra a compilação** de `LoginHandler`, `RegisterUserHandler`, `GetCurrentUserHandler`, `DbSeeder` e `UsuarioConfiguration` (todos referenciam `usuario.Role` ou `UserRole`). **Isso é esperado e será corrigido em tasks seguintes (até a Task 7).** NÃO tente corrigir esses consumidores nesta task. NÃO rode `dotnet build` da solução inteira — ele vai falhar de propósito. Rode apenas o teste filtrado desta task (`--filter RefreshTokenTests`), que compila só o necessário? Observação: o projeto de testes referencia Domain+Application; se o filtro não compilar por causa da quebra em Application, rode o build só do Domain para validar as entidades (`dotnet build src/GatumsGourmet.Domain`) e reporte isso como concern — o teste roda de fato na Task 7 quando a solução voltar a compilar.

## Commit
`feat: entidades de RBAC e refresh token no domain`

## Não dispare subagentes. Faça tudo você mesmo. Não rode build da solução inteira (quebra intencional).
