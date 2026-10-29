# Task 15: Remover enum UserRole legado, fechar DI, validar suíte completa

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 143 unit + 95 integração verdes.

**Objetivo:** aposentar o enum legado `UserRole`, que só sobrevive na definição e numa conveniência de teste. Deixar a solução limpa e rodar a suíte inteira como checkpoint.

## Estado atual (grep de UserRole)
Só há DUAS referências ao enum em todo o `.cs`:
1. `src/GatumsGourmet.Domain/Enums/Auth/UserRole.cs` — a definição: `public enum UserRole { Admin, Cliente }`.
2. `tests/GatumsGourmet.IntegrationTests/Factory/AuthenticatedClientFactory.cs` — usa `UserRole.Admin`/`UserRole.Cliente` como parâmetro de conveniência em `CreateAdminClientAsync`/`CreateClienteClientAsync`/`CreateAuthenticatedClientAsync(UserRole role)`.

Nenhum código de PRODUÇÃO referencia mais `UserRole` (foi substituído por roles em tabela). O enum é dead code exceto pela factory de teste.

## Trabalho

### 1. Refatorar `AuthenticatedClientFactory.cs` para não depender de `UserRole`
Trocar o parâmetro `UserRole role` por `string roleNome` (os nomes de role "Admin"/"Cliente" em PascalCase, como no seed). Ajuste:
- `CreateAdminClientAsync` → `CreateAuthenticatedClientAsync("Admin")`.
- `CreateClienteClientAsync` → `CreateAuthenticatedClientAsync("Cliente")`.
- `CreateAuthenticatedClientAsync(this CustomWebApplicationFactory factory, string roleNome)`:
  - Remover `using GatumsGourmet.Domain.Enums.Auth;`.
  - Onde hoje faz `var roleNome = role.ToString();` e `var roleLower = role.ToString().ToLower();` — agora `roleNome` já é o parâmetro; `roleLower = roleNome.ToLowerInvariant()` (usado só para compor email/nome de teste).
  - O resto (buscar/criar a Role por nome, vincular UsuarioRole, login real) permanece igual.
  - O record `AuthenticatedUser(Guid Id, string Email, string Role, string Token)` — o campo `Role` continua string; passe `roleLower` ou `roleNome` como antes (mantenha o comportamento atual; só não dependa do enum).
Garanta que NÃO quebra os ~30 testes que chamam `CreateAdminClientAsync()`/`CreateClienteClientAsync()` (sem argumento) — essas assinaturas sem parâmetro permanecem.

### 2. Remover o enum
Deletar `src/GatumsGourmet.Domain/Enums/Auth/UserRole.cs` (e a pasta `Enums/Auth/` se ficar vazia — verifique se há outros enums lá; provavelmente só UserRole).

### 3. Validar (checkpoint da fundação completa)
- `dotnet build GatumsGourmet.sln` → **0 erros**.
- `dotnet test tests/GatumsGourmet.UnitTests` → todos verdes (reporte contagem).
- `dotnet test tests/GatumsGourmet.IntegrationTests` → todos verdes (container de pé; reporte contagem).
Se algo quebrar, investigue a causa raiz (provável: referência residual ao enum ou algum teste que assumia o enum) e corrija. NÃO mascare.

## Commit
`chore: remover UserRole legado e fechar DI de auth`

## Não dispare subagentes. Confirme com grep final que não resta nenhuma referência a UserRole em .cs.
