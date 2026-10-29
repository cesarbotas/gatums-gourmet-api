# Task 15 — Report: Remover enum UserRole legado, fechar DI de auth, validar suíte completa

**Branch:** feature/AjusteLoginToken
**Status:** Concluída ✅

## 1. Refatoração de `AuthenticatedClientFactory.cs`
Arquivo: `tests/GatumsGourmet.IntegrationTests/Factory/AuthenticatedClientFactory.cs`

- Removido o `using GatumsGourmet.Domain.Enums.Auth;`.
- `CreateAuthenticatedClientAsync(this CustomWebApplicationFactory factory, UserRole role)` → agora recebe `string roleNome`.
- `CreateAdminClientAsync()` → chama `CreateAuthenticatedClientAsync("Admin")`.
- `CreateClienteClientAsync()` → chama `CreateAuthenticatedClientAsync("Cliente")`.
- Substituído `var roleNome = role.ToString();` / `var roleLower = role.ToString().ToLower();` por `var roleLower = roleNome.ToLowerInvariant();` (`roleNome` agora é o próprio parâmetro).
- Comportamento preservado: busca/cria a `Role` por nome (`db.Roles.FirstOrDefaultAsync(r => r.Nome == roleNome)` ou cria), vincula via `UsuarioRole`, faz login real em `/api/v1/auth/login` e usa o token retornado. O campo `Role` do record `AuthenticatedUser` continua recebendo `roleLower` (comportamento idêntico ao anterior).
- **Assinaturas sem argumento mantidas** (`CreateAdminClientAsync()` / `CreateClienteClientAsync()`) — os ~30 testes que as chamam não foram afetados.

## 2. Remoção do enum
- Deletado `src/GatumsGourmet.Domain/Enums/Auth/UserRole.cs` (era `public enum UserRole { Admin, Cliente }`).
- A pasta `src/GatumsGourmet.Domain/Enums/Auth/` ficou vazia (continha apenas `UserRole.cs`) e foi removida. Os demais enums vivem em pastas irmãs (`Pagamentos`, `Pedidos`, `Produtos`), intactas.

## 3. Grep final
`grep "UserRole" em **/*.cs` → **nenhuma ocorrência**. Nenhum código de produção ou teste referencia mais o enum.

## 4. Build + suítes de teste
- `dotnet build GatumsGourmet.sln` → **0 erros** (6 avisos pré-existentes, não relacionados: NU1902/NU1903 de `System.Security.Cryptography.Pkcs` e MSB3277 de conflito de versão do EF Core Relational).
- `dotnet test tests/GatumsGourmet.UnitTests` → **143 aprovados / 0 falhas / 0 ignorados** (Total 143).
- `dotnet test tests/GatumsGourmet.IntegrationTests` → **95 aprovados / 0 falhas / 0 ignorados** (Total 95). Container Postgres de teste (`gatums-gourmet-db-test-integration`, porta 5433) de pé e healthy.

Nenhuma quebra investigada — a suíte passou integralmente de primeira após a refatoração.

## 5. Arquivos alterados
| Arquivo | Mudança |
|---|---|
| `tests/GatumsGourmet.IntegrationTests/Factory/AuthenticatedClientFactory.cs` | Modificado — parâmetro `UserRole` → `string`, removido using do enum |
| `src/GatumsGourmet.Domain/Enums/Auth/UserRole.cs` | Deletado |

## 6. Auto-revisão do diff
- A mudança é mínima e cirúrgica: só os dois arquivos previstos no brief.
- O contrato público de teste (métodos sem argumento) permanece idêntico — compatível com os chamadores existentes.
- `ToLowerInvariant()` é mais correto que o antigo `ToLower()` dependente de cultura; como só compõe email/nome de teste, não altera comportamento observável.
- O nome de role em PascalCase ("Admin"/"Cliente") casa com o seed e com a busca por `r.Nome == roleNome`.
- Nenhuma dependência residual do enum em produção ou teste (confirmado por grep).

## Concerns
- Avisos pré-existentes (NU1902/NU1903 vulnerabilidade em `System.Security.Cryptography.Pkcs` 9.0.0; MSB3277 conflito de versão EF Core Relational 10.0.4 vs 10.0.13) continuam no build; fora do escopo desta task, mas vale endereçar em task futura.
- A factory agora aceita qualquer string como role; não há validação contra roles inexistentes (cria a role on-the-fly, que é o comportamento desejado para o seed de teste).
