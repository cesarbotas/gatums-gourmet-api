# Task 6 — Relatório: Migration RBAC + seed + data migration do enum legado

**Status:** DONE (com fixes de integração cross-task — ver Concerns)
**Branch:** feature/AjusteLoginToken

## 1. Migration gerada

Comando:
```
dotnet ef migrations add AuthRbacRefreshToken -p src/GatumsGourmet.Infrastructure -s src/GatumsGourmet.Api
```
Gerou `20261026143118_AuthRbacRefreshToken.cs` + `.Designer.cs` e atualizou `GatumsDbContextModelSnapshot.cs`.

dotnet ef disponível: **10.0.13**. Container de teste (gatums-gourmet-db-test-integration, porta 5433) confirmado rodando (healthy).

Nomes reais confirmados no snapshot (PostgreSQL, PascalCase com aspas duplas):
`"Permissions"`, `"Roles"`, `"RolePermissions"`, `"UsuarioRoles"`, `"RefreshTokens"`, `"Usuarios"`.

## 2. Edições no `Up` (ordem crítica, idempotente)

O EF gerou o `DropColumn("Role")` no **início**. Reescrevi o `Up` na ordem correta:

1. `ADD COLUMN "Ativo" boolean NOT NULL DEFAULT TRUE`.
2. `CREATE TABLE` das 5 tabelas + índices (mantido como gerado).
3. **Seed idempotente** (`migrationBuilder.Sql`):
   - 12 permissões → `INSERT ... ON CONFLICT ("Codigo") DO NOTHING`.
   - 3 roles (Admin/Cliente Sistema=true, Gerente Sistema=false) → `ON CONFLICT ("Nome") DO NOTHING`.
   - Admin: `CROSS JOIN Permissions` (todas as 12) → `ON CONFLICT DO NOTHING`.
   - Gerente: `JOIN` por 8 `Codigo` específicos → `ON CONFLICT DO NOTHING`.
   - Cliente: nenhum insert (0 permissões).
4. **Data migration do enum legado** protegida por `DO $$ ... IF EXISTS (information_schema.columns WHERE table_name='Usuarios' AND column_name='Role') ... END $$`:
   - `UPDATE "Usuarios" SET "Ativo"=true WHERE "Ativo" IS DISTINCT FROM true`.
   - `INSERT INTO "UsuarioRoles" SELECT u."Id", r."Id" ... JOIN "Roles" r ON r."Nome"=u."Role" ON CONFLICT DO NOTHING`.
   - A referência a `u."Role"` só é compilada/executada dentro do guard → bancos criados do zero (sem a coluna) pulam sem erro.
5. **DropColumn por último**: `ALTER TABLE "Usuarios" DROP COLUMN IF EXISTS "Role"`.

Verificado via `dotnet ef migrations script` que a ordem final do SQL é exatamente essa (tudo dentro de START TRANSACTION/COMMIT).

## 3. Edições no `Down`

- Recria `"Role"` (text, **nullable**).
- Copia de volta a role de **maior privilégio** por usuário (Admin > Gerente > Cliente) via `ARRAY_AGG` ordenado + `[1]`.
- Dropa as 5 tabelas e a coluna `Ativo`.
- Comentário documenta que **roles múltiplas se perdem** no rollback (modelo legado só tinha uma role por usuário).

## 4. DbSeeder

`src/GatumsGourmet.Infrastructure/Data/DbSeeder.cs`: após criar o Admin inicial (quando `!Usuarios.AnyAsync()`), faz `SaveChangesAsync`, busca a role `Admin` (semeada pela migration, que roda antes via `MigrateAsync`) e insere o vínculo em `UsuarioRoles` — com guard `!AnyAsync(...)` para idempotência.

## 5. Teste de integração

`tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Auth/MigrationSeedTests.cs`, `[Collection("Integration")]`, resolve o `GatumsDbContext` via `factory.Services.CreateScope()`. 4 fatos (idempotentes, verificam estado — não assumem banco vazio):
1. 3 roles (Admin/Cliente Sistema=true, Gerente Sistema=false).
2. 12 permissões por `Codigo`.
3. RolePermissions: Admin=12, Cliente=0, Gerente=8.
4. admin@gatums.com vinculado à role Admin via UsuarioRoles.

## 6. Validação

- `dotnet build GatumsGourmet.sln` → **0 erros** (apenas avisos MSB3277 de unificação de versão, pré-existentes).
- `dotnet test tests/GatumsGourmet.IntegrationTests` → **71/71 aprovados, 0 falhas** (MigrationSeedTests 4/4). A suíte rodou múltiplas vezes contra o banco persistente → seed/migration idempotentes confirmados.
- `dotnet test tests/GatumsGourmet.UnitTests` → **119/119 aprovados** (sem regressão).

### Investigação de regressão (obrigatória pelo brief)
Primeira execução da suíte de integração: **43 falhas**. Root cause = **mismatch de casing de role** entre a camada de autorização (Task 5) e a convenção PascalCase do RBAC (brief): roles/permissões são semeadas como `Admin`/`Cliente`/`Gerente` (PascalCase), o `PermissionResolver` e o `RegisterUserHandler` usam PascalCase, mas as policies e o helper usavam lowercase. Minha seed foi a primeira vez que dados reais de role existiram, expondo o bug. Fixes mínimos e corretos (não mascaram nada — alinham o casing ao contrato do brief):
- `AuthenticationExtensions.cs`: `RequireRole("admin"/"cliente")` → `RequireRole("Admin"/"Cliente")` (resolveu ~40 falhas 403 Forbidden).
- `ClaimsPrincipalExtensions.IsAdmin()`: `IsInRole("admin")` → `IsInRole("Admin")` (resolveu 2 falhas de Pedidos — admin tratado como não-admin).
- `LoginEndpointTests.cs`: teste **stale** do contrato pré-Task-5 (lia `token` + `user.role` string). Atualizado para o contrato atual (`accessToken` + `user.roles[]` contendo `"Cliente"`).

## Arquivos alterados
- `src/GatumsGourmet.Infrastructure/Migrations/20261026143118_AuthRbacRefreshToken.cs` (novo, editado Up/Down)
- `src/GatumsGourmet.Infrastructure/Migrations/20261026143118_AuthRbacRefreshToken.Designer.cs` (novo, gerado)
- `src/GatumsGourmet.Infrastructure/Migrations/GatumsDbContextModelSnapshot.cs` (regenerado)
- `src/GatumsGourmet.Infrastructure/Data/DbSeeder.cs` (vínculo Admin→role Admin)
- `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Auth/MigrationSeedTests.cs` (novo)
- `src/GatumsGourmet.Api/Extensions/AuthenticationExtensions.cs` (fix casing policy)
- `src/GatumsGourmet.Api/Extensions/ClaimsPrincipalExtensions.cs` (fix casing IsAdmin)
- `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Auth/Login/LoginEndpointTests.cs` (contrato atual)

## Auto-revisão
- Ordem Up/Down verificada no SQL gerado (`dotnet ef migrations script`): seed → data migration guarded → DropColumn por último; tudo transacional.
- Idempotência: todos os INSERTs com `ON CONFLICT DO NOTHING`; DbSeeder e teste verificam estado. Suíte reexecutada sobre banco persistente sem falhas.
- Data migration segura para banco novo (coluna `Role` inexistente) via guard em `information_schema`.
- `Down` recria coluna nullable e preserva a role de maior privilégio; perda de roles múltiplas documentada.

## Concerns
1. **Fixes fora do escopo estrito da Task 6** em 3 arquivos da Task 5 (2 de produção: AuthenticationExtensions, ClaimsPrincipalExtensions; 1 de teste: LoginEndpointTests). Eram bloqueantes — sem eles a suíte de integração ficava em 43 falhas por causa do casing. São correções corretas (alinham ao PascalCase mandado pelo brief), não workarounds. Recomendo validação do revisor por tocarem a camada de autorização.
2. Convenção de casing de role ("Admin" vs "admin") deveria ser padronizada/centralizada (constantes) para evitar reincidência — fora do escopo desta task.
