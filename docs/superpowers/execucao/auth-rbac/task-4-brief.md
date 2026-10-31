# Task 4: Configurações EF e DbSets do RBAC/refresh

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Camada Infrastructure, EF Core + Npgsql.

**Objetivo:** criar as configs EF das 5 entidades novas, ajustar a `UsuarioConfiguration`, e registrar os DbSets + ApplyConfiguration no `GatumsDbContext`.

## Constraints globais
- **Uma config por arquivo**, em `src/GatumsGourmet.Infrastructure/Data/Configurations/Auth/`.
- Seguir o padrão das configs existentes (ver `UsuarioConfiguration.cs`, abaixo). `IEntityTypeConfiguration<T>`, namespace `GatumsGourmet.Infrastructure.Data.Configurations.Auth`.
- net10.0, noBOM, ferramentas de escrita de arquivo.
- Entidades (Task 1): Role, Permission, RolePermission, UsuarioRole, RefreshToken, Usuario (já sem o campo Role, com Ativo e UsuarioRoles).

## Estado atual
`UsuarioConfiguration.cs` HOJE (tem `b.Property(x => x.Role).HasConversion<string>();` que precisa SAIR, pois a entidade não tem mais `Role`):
```csharp
using GatumsGourmet.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace GatumsGourmet.Infrastructure.Data.Configurations.Auth;
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.SenhaHash).IsRequired();
        b.Property(x => x.Role).HasConversion<string>();   // <-- REMOVER esta linha
    }
}
```

`GatumsDbContext.cs` tem os DbSets e um `OnModelCreating` que faz `ApplyConfiguration` de cada config. Há também um `GatumsDbContextFactory` (design-time) — não mexer nele.

## Trabalho

### 1. Criar 5 configs em `Data/Configurations/Auth/` (1 por arquivo):

- `RoleConfiguration.cs` (`IEntityTypeConfiguration<Role>`):
  - HasKey(Id); `Nome` HasMaxLength(100) IsRequired; `HasIndex(Nome).IsUnique()`; `Descricao` HasMaxLength(300); `Sistema` default false.
  - Relação N:N com Permission via RolePermission e com Usuario via UsuarioRole já declaradas nas configs de junção abaixo — aqui não precisa reconfigurar.

- `PermissionConfiguration.cs` (`IEntityTypeConfiguration<Permission>`):
  - HasKey(Id); `Codigo` HasMaxLength(100) IsRequired; `HasIndex(Codigo).IsUnique()`; `Recurso` HasMaxLength(50) IsRequired; `Acao` HasMaxLength(50) IsRequired; `Descricao` HasMaxLength(300).

- `RolePermissionConfiguration.cs` (`IEntityTypeConfiguration<RolePermission>`):
  - `HasKey(x => new { x.RoleId, x.PermissionId })` (PK composta).
  - `HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade)`.
  - `HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade)`.

- `UsuarioRoleConfiguration.cs` (`IEntityTypeConfiguration<UsuarioRole>`):
  - `HasKey(x => new { x.UsuarioId, x.RoleId })` (PK composta).
  - `HasOne(x => x.Usuario).WithMany(u => u.UsuarioRoles).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade)`.
  - `HasOne(x => x.Role).WithMany(r => r.UsuarioRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade)`.

- `RefreshTokenConfiguration.cs` (`IEntityTypeConfiguration<RefreshToken>`):
  - HasKey(Id); `TokenHash` HasMaxLength(128) IsRequired; `HasIndex(TokenHash).IsUnique()`; `HasIndex(UsuarioId)`; `HasIndex(FamiliaId)`; `MotivoRevogacao` HasMaxLength(200); `SubstituidoPorTokenHash` HasMaxLength(128); `CriadoPorIp` HasMaxLength(64).
  - FK para Usuario por UsuarioId (sem navegação na entidade RefreshToken — use `HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade)`).

### 2. Ajustar `UsuarioConfiguration.cs`:
- Remover a linha `b.Property(x => x.Role).HasConversion<string>();`.
- Adicionar `b.Property(x => x.Ativo).HasDefaultValue(true);` (a navegação UsuarioRoles é configurada em UsuarioRoleConfiguration).

### 3. Ajustar `GatumsDbContext.cs`:
- Adicionar DbSets: `Roles`, `Permissions`, `RolePermissions`, `UsuarioRoles`, `RefreshTokens` (com os tipos corretos; siga o padrão `public DbSet<Role> Roles => Set<Role>();`).
- Em `OnModelCreating`, adicionar `modelBuilder.ApplyConfiguration(new RoleConfiguration());` e as outras 4 (RolePermission, Permission, UsuarioRole, RefreshToken).
- NÃO mexer no `GatumsDbContextFactory`.

## Validação
`dotnet build src/GatumsGourmet.Infrastructure`. A Application continua quebrada de propósito (Task 7) — se o build transitivo falhar só por erros em arquivos da Application (Usuario.Role em LoginHandler etc.), isso é esperado; confirme que NÃO há erros em arquivos de Infrastructure/Configurations. Reporte como concern. NÃO rode build da solução inteira nem corrija a Application.

## Commit
`feat: configuracoes EF e DbSets de RBAC/refresh`

## Não dispare subagentes. Não corrija a Application. Não mexa no GatumsDbContextFactory.
