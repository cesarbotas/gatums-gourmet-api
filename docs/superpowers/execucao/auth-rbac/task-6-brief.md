# Task 6: Migration transacional + data migration + seed de roles/permissões + MigrationSeedTests

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. EF Core + Npgsql (PostgreSQL).

**Objetivo:** gerar a migration EF que cria as tabelas de RBAC/refresh, semeia roles/permissões de sistema, migra o enum legado de role (coluna `Role` que pode existir no banco antigo) para `UsuarioRoles`, e remove a coluna legada. Ajustar o `DbSeeder` para vincular o Admin inicial à role Admin. Escrever um teste de integração que valida o seed.

**Pré-condição:** a solução compila (Task 7 concluída). O container de teste Postgres (gatums-gourmet-db-test-integration, porta 5433) está rodando.

## Constraints globais
- net10.0, noBOM. EF Core migrations.
- Catálogo de permissões e roles — fonte: a SDD (`docs/superpowers/specs/2026-10-21-auth-rbac-refresh-token-design.md`, seção 3). Resumo abaixo.

## Catálogo de permissões (12) — formato recurso:acao
produtos:ler, produtos:escrever, categorias:escrever, clientes:ler, clientes:escrever, pedidos:ler, pedidos:avancar, pagamentos:validar, relatorios:ler, usuarios:gerenciar, roles:gerenciar, permissoes:gerenciar.
(Recurso e Acao derivam do código: ex. "produtos:escrever" → Recurso="produtos", Acao="escrever". Descricao: frase curta em PT.)

## Roles de sistema (3)
- **Admin** (Sistema=true): TODAS as 12 permissões.
- **Cliente** (Sistema=true): NENHUMA permissão.
- **Gerente** (Sistema=false): produtos:escrever, categorias:escrever, clientes:ler, clientes:escrever, pedidos:ler, pedidos:avancar, relatorios:ler, pagamentos:validar.

## Trabalho

### 1. Gerar a migration
`dotnet ef migrations add AuthRbacRefreshToken -p src/GatumsGourmet.Infrastructure -s src/GatumsGourmet.Api`
Isso gera o CreateTable das 5 tabelas (Roles, Permissions, RolePermissions, UsuarioRoles, RefreshTokens), a coluna `Ativo` em Usuarios, e provavelmente um `DropColumn` de `Role` (já que a entidade não tem mais o campo) — o EF detecta isso a partir do snapshot. CONFIRA o `Up` gerado.

### 2. Editar o `Up` da migration (ordem crítica, idempotente)
Após os `CreateTable`/`AddColumn` que o EF gerou, e ANTES do `DropColumn("Role", "Usuarios")` (mova o DropColumn para o FINAL se o EF o colocou no começo):
a) **Seed de roles e permissões** via `migrationBuilder.InsertData` ou SQL (`migrationBuilder.Sql`). Como os Ids de Role/Permission são identity (int), prefira **SQL idempotente** que insere por `Codigo`/`Nome` e resolve os vínculos por subselect. Exemplo de abordagem:
   - `INSERT INTO "Permissions" ("Codigo","Recurso","Acao","Descricao") VALUES (...) ON CONFLICT ("Codigo") DO NOTHING;` (12 linhas).
   - `INSERT INTO "Roles" ("Nome","Descricao","Sistema") VALUES ('Admin',...,true),('Cliente',...,true),('Gerente',...,false) ON CONFLICT ("Nome") DO NOTHING;`
   - Vínculos: `INSERT INTO "RolePermissions" ("RoleId","PermissionId") SELECT r."Id", p."Id" FROM "Roles" r, "Permissions" p WHERE r."Nome"='Admin' ON CONFLICT DO NOTHING;` (Admin = todas); para Gerente, `... WHERE r."Nome"='Gerente' AND p."Codigo" IN (...8 códigos...)`.
   (Confirme os nomes reais de tabelas/colunas no snapshot gerado — PascalCase com aspas duplas no Postgres.)
b) **Data migration do enum legado**: migrar usuários que tinham a coluna `Role` (valores 'Admin'/'Cliente' como texto, pois era HasConversion<string>) para `UsuarioRoles`. Como o `DropColumn` ainda não rodou, a coluna `Role` existe neste ponto:
   `INSERT INTO "UsuarioRoles" ("UsuarioId","RoleId") SELECT u."Id", r."Id" FROM "Usuarios" u JOIN "Roles" r ON r."Nome" = u."Role" ON CONFLICT DO NOTHING;`
   E `UPDATE "Usuarios" SET "Ativo" = true WHERE "Ativo" IS NOT DISTINCT FROM false;` (ou simplesmente garantir Ativo=true para todos os existentes — a coluna tem default true, mas linhas pré-existentes precisam do UPDATE).
   IMPORTANTE: se o banco de teste já não tiver a coluna `Role` (bancos criados do zero após a Task 1 nunca a tiveram), o `JOIN u."Role"` falha. Proteja com checagem: envolva a data migration num bloco condicional que só roda se a coluna existir — use `migrationBuilder.Sql` com um `DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Usuarios' AND column_name='Role') THEN ... END IF; END $$;`.
c) **DropColumn** `Role` de `Usuarios` por último (o EF provavelmente já gerou; garanta que fica DEPOIS da data migration).

### 3. Editar o `Down` da migration
Recriar a coluna `Role` (text, nullable), copiar de volta a role de maior privilégio de cada usuário (`UPDATE ... SET Role = 'Admin' WHERE existe vínculo Admin; senão 'Cliente'`), e dropar as 5 tabelas. Documente no comentário que roles múltiplas se perdem no rollback.

### 4. Ajustar `DbSeeder.cs`
O seed do Admin hoje cria o usuário SEM role. Ajuste: após criar o admin (quando `!await db.Usuarios.AnyAsync()`), dar `SaveChangesAsync`, buscar a role Admin (`db.Roles.FirstOrDefaultAsync(r => r.Nome == "Admin")`) e inserir o vínculo em `db.UsuarioRoles` (UsuarioId do admin recém-criado + RoleId). Como as roles são semeadas pela migration, elas já existem quando o seeder roda (o seeder chama MigrateAsync primeiro). Mantenha a idempotência (não duplica se já houver usuário).

### 5. Teste de integração `MigrationSeedTests`
`tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Auth/MigrationSeedTests.cs` (ou pasta Auth apropriada), `[Collection("Integration")]`, recebe `CustomWebApplicationFactory`.
- Resolve o `GatumsDbContext` via `factory.Services.CreateScope()` → `GetRequiredService<GatumsDbContext>()`.
- Assertivas (idempotentes — o banco de teste persiste entre execuções; verifique estado, não assuma banco vazio):
  1. Existem as 3 roles: Admin, Cliente, Gerente. Admin e Cliente com `Sistema=true`.
  2. Existem as 12 permissões (conte por `Codigo`).
  3. Admin tem 12 vínculos em RolePermissions; Cliente tem 0; Gerente tem 8.
  4. Existe ao menos 1 usuário com email admin@gatums.com vinculado à role Admin (via UsuarioRoles).
- Use FluentAssertions.

## Validação
1. `dotnet build GatumsGourmet.sln` → 0 erros.
2. `dotnet test tests/GatumsGourmet.IntegrationTests` → MigrationSeedTests passa (container de teste de pé). Rode a suíte de integração inteira e reporte a contagem; se algum teste pré-existente quebrar por causa do novo schema/seed, investigue e reporte.

## Commit
`feat: migration RBAC com seed e data migration do enum legado`

## IMPORTANTE
- A migration roda automaticamente no startup (DatabaseExtensions/DbSeeder) — por isso o teste de integração exercita a migration de verdade.
- Preste atenção aos nomes reais de tabela/coluna no snapshot gerado (PascalCase, aspas duplas no Postgres).
- Se o `dotnet ef` não estiver disponível, reporte BLOCKED com o erro (pode precisar de `dotnet tool install --global dotnet-ef` ou usar o que já existe).

## Não dispare subagentes. Faça tudo você mesmo.
