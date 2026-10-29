# Task 4 — Configurações EF e DbSets do RBAC/refresh — Report

**Status:** DONE_WITH_CONCERNS (concern é apenas a quebra intencional da Application — Task 7)
**Branch:** feature/AjusteLoginToken
**Commit:** `dbed189` — feat: configuracoes EF e DbSets de RBAC/refresh

## O que foi criado

5 configs novas em `src/GatumsGourmet.Infrastructure/Data/Configurations/Auth/` (1 por arquivo), todas `IEntityTypeConfiguration<T>` no namespace `GatumsGourmet.Infrastructure.Data.Configurations.Auth`:

- **RoleConfiguration.cs** — HasKey(Id); Nome max 100 required; HasIndex(Nome).IsUnique(); Descricao max 300; Sistema HasDefaultValue(false).
- **PermissionConfiguration.cs** — HasKey(Id); Codigo max 100 required; HasIndex(Codigo).IsUnique(); Recurso max 50 required; Acao max 50 required; Descricao max 300.
- **RolePermissionConfiguration.cs** — PK composta (RoleId, PermissionId); FK Role→RolePermissions (Cascade); FK Permission→RolePermissions (Cascade).
- **UsuarioRoleConfiguration.cs** — PK composta (UsuarioId, RoleId); FK Usuario→UsuarioRoles (Cascade); FK Role→UsuarioRoles (Cascade).
- **RefreshTokenConfiguration.cs** — HasKey(Id); TokenHash max 128 required + unique; HasIndex(UsuarioId); HasIndex(FamiliaId); MotivoRevogacao max 200; SubstituidoPorTokenHash max 128; CriadoPorIp max 64; FK sem navegação `HasOne<Usuario>().WithMany().HasForeignKey(UsuarioId).OnDelete(Cascade)`.

## O que foi ajustado

- **UsuarioConfiguration.cs** — removida a linha `b.Property(x => x.Role).HasConversion<string>();` (entidade não tem mais Role); adicionada `b.Property(x => x.Ativo).HasDefaultValue(true);`.
- **GatumsDbContext.cs** — adicionados 5 DbSets (`Roles`, `Permissions`, `RolePermissions`, `UsuarioRoles`, `RefreshTokens`) no padrão `=> Set<T>()`; adicionados 5 `ApplyConfiguration` no `OnModelCreating` (Role, Permission, RolePermission, UsuarioRole, RefreshToken). `GatumsDbContextFactory` NÃO foi tocado.

## Resultado do build

`dotnet build src/GatumsGourmet.Infrastructure` → FAILED, mas **0 erros em arquivos de Infrastructure/Configurations**. Todos os erros são da Application (quebra intencional da Task 7):
- `Login/LoginHandler.cs` (CS1061 Usuario.Role)
- `GetCurrentUser/GetCurrentUserHandler.cs` (CS1061 Usuario.Role)
- `RegisterUser/RegisterUserHandler.cs` (CS0117 Usuario.Role)

Nenhuma config/entidade foi alterada na Application. Build transitivo falha só por esses handlers, como esperado.

## Auto-revisão do diff

`git diff --cached --stat`: 7 arquivos, +117 / -1. Confirei:
- Nomes de propriedades batem com as entidades (Role.Nome/Descricao/Sistema, Permission.Codigo/Recurso/Acao/Descricao, RolePermission.RoleId/PermissionId, UsuarioRole.UsuarioId/RoleId, RefreshToken.*).
- PKs compostas e navegações (RolePermissions/UsuarioRoles) existem nas entidades — sem risco de shadow property.
- RefreshToken usa FK sem navegação conforme brief (entidade não tem nav para Usuario).
- Uma config por arquivo; padrão idêntico ao UsuarioConfiguration existente.

## Concerns

1. **Application quebrada (Task 7)** — esperado; não corrigido por instrução. 3 handlers referenciam `Usuario.Role` que não existe mais.
2. **Warnings LF→CRLF** do git ao adicionar os arquivos noBOM — cosmético, normalização de fim de linha no Windows; sem impacto.
