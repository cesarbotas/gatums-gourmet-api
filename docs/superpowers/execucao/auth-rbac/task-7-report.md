# Task 7 — Report: Login, Register, GetCurrentUser com novo esquema (fecha a quebra de build)

**Status:** DONE
**Branch:** feature/AjusteLoginToken

## Resumo
Reescritos os 3 handlers de auth para o novo modelo RBAC (sem `Usuario.Role`), com
access+refresh no login. A quebra de build iniciada na Task 1 foi fechada: a solução
inteira compila com 0 erros e todos os testes unitários passam, incluindo os das
Tasks 1/3 que antes não rodavam.

## DTOs alterados (novo shape)
- `Login/LoginResponse.cs`
  - `LoginUserDto(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles)`
  - `LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, LoginUserDto User)`
- `GetCurrentUser/GetCurrentUserResponse.cs` → `(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles)`
- `RegisterUser/RegisterUserResponse.cs` → `(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles)`

## Handlers alterados
- **LoginHandler**: injeta `IUsuarioRepository, IPasswordHasher, IJwtTokenGenerator,
  IRefreshTokenGenerator, IRefreshTokenStore, IPermissionResolver, IClock, IConfiguration`.
  Fluxo: credenciais → **rejeita usuário inativo** (`!usuario.Ativo` → "Usuário inativo.")
  → resolve roles/permissões → gera access token → gera refresh token e grava
  (`AddAsync` + `SaveChangesAsync`) → `expiresIn = AccessTokenMinutes*60` → retorna
  `LoginResponse`. `RefreshTokenDays` default 7, `AccessTokenMinutes` default 15.
- **RegisterUserHandler**: injeta `IUsuarioRepository, IRoleRepository, IPasswordHasher,
  IClock`. Cria usuário `Ativo=true`, salva, busca role "Cliente" via `GetByNomeAsync`
  e, se existir, vincula via `SetRolesAsync`. Retorna roles `["Cliente"]`.
- **GetCurrentUserHandler**: usa `GetByIdComRolesAsync` e projeta
  `usuario.UsuarioRoles.Select(ur => ur.Role.Nome)`.

## DI
Nenhuma mudança necessária em `ApplicationServiceCollectionExtensions` nem em
`InfrastructureServiceCollectionExtensions`. Os serviços injetados pelos handlers
(`IJwtTokenGenerator`, `IRefreshTokenGenerator`, `IRefreshTokenStore`,
`IPermissionResolver`, `IRoleRepository`, `IPasswordHasher`, `IClock`) já estão
registrados pela Infra; `IConfiguration` é provido pelo host. Verificado.

Adicionada ao `GatumsGourmet.Application.csproj` a referência
`Microsoft.Extensions.Configuration.Abstractions` (+ `.Binder`) 10.0.13 — necessária
porque o LoginHandler passou a injetar `IConfiguration` e a usar `GetValue<int?>`
(a camada Application não as referenciava).

## Evidência TDD RED → GREEN
- **RED (antes):** `dotnet build GatumsGourmet.sln` falhava com 4 erros CS em
  GetCurrentUserHandler, LoginHandler (x2) e RegisterUserHandler — todos por
  `Usuario` não conter mais a definição `Role` (removida na Task 1). O projeto
  Application interrompia a solução antes de Infra/testes compilarem.
- Testes reescritos primeiro (LoginHandlerTests com 4 casos incl. UsuarioInativo;
  GetCurrentUser e RegisterUser ajustados ao novo shape) — não compilavam contra os
  handlers antigos.
- **GREEN (depois):** implementados DTOs + handlers; build 0 erros; testes verdes.

## Resultado final
- `dotnet build GatumsGourmet.sln` → **0 erros**, 6 avisos. (Compilação com êxito.)
- `dotnet test tests/GatumsGourmet.UnitTests` → **119 aprovados, 0 falhas, 0 ignorados**.
  Inclui RefreshTokenTests, JwtTokenGeneratorTests, RefreshTokenGeneratorTests
  (Tasks 1/3, antes não executáveis) e os 4 testes de Login reescritos.

## Arquivos alterados (13)
src:
- src/GatumsGourmet.Application/GatumsGourmet.Application.csproj
- src/GatumsGourmet.Application/UseCases/V1/Auth/Login/LoginHandler.cs
- src/GatumsGourmet.Application/UseCases/V1/Auth/Login/LoginResponse.cs
- src/GatumsGourmet.Application/UseCases/V1/Auth/RegisterUser/RegisterUserHandler.cs
- src/GatumsGourmet.Application/UseCases/V1/Auth/RegisterUser/RegisterUserResponse.cs
- src/GatumsGourmet.Application/UseCases/V1/Auth/GetCurrentUser/GetCurrentUserHandler.cs
- src/GatumsGourmet.Application/UseCases/V1/Auth/GetCurrentUser/GetCurrentUserResponse.cs
- src/GatumsGourmet.Infrastructure/Data/DbSeeder.cs

tests:
- tests/GatumsGourmet.UnitTests/.../Auth/Login/LoginHandlerTests.cs
- tests/GatumsGourmet.UnitTests/.../Auth/GetCurrentUser/GetCurrentUserHandlerTests.cs
- tests/GatumsGourmet.UnitTests/.../Auth/RegisterUser/RegisterUserHandlerTests.cs
- tests/GatumsGourmet.IntegrationTests/.../Auth/GetCurrentUser/GetCurrentUserHandlerTests.cs
- tests/GatumsGourmet.IntegrationTests/Factory/AuthenticatedClientFactory.cs

## Auto-revisão
- Login rejeita inativo ANTES de gerar tokens; `refreshStore.AddAsync` não é chamado
  em nenhum caminho de falha (verificado por testes Inexistente/SenhaIncorreta/Inativo).
- Register persiste o usuário mesmo quando a role "Cliente" não existe (vinculação
  condicional, conforme brief) e retorna `["Cliente"]` fixo.
- Config lida com `GetValue<int?>` + fallback 7/15 (robusto a chave ausente).

## Concerns (fora do escopo do brief, mas necessários p/ fechar o build)
1. **DbSeeder.cs** usava `Usuario.Role`; removi o campo e setei `Ativo=true`. O usuário
   Admin semeado NÃO recebe vínculo de role (não há seeding de Roles/Permissions ainda).
   O admin não terá claims de role em runtime até existir um seeder de RBAC — provável
   tarefa futura, não coberta pela Task 7.
2. **Testes de integração** (`AuthenticatedClientFactory`, `GetCurrentUser` integ.)
   referenciavam `Usuario.Role`/`"token"`. Ajustados para compilar e para o novo
   esquema (vínculo via `UsuarioRoles`, campo `accessToken`). Não executados aqui
   (dependem de Postgres/Docker); apenas garantido que compilam.
3. **Divergência de nomenclatura de role** fora do escopo: policies em
   `AuthenticationExtensions`/`ClaimsPrincipalExtensions` usam "admin"/"cliente"
   (minúsculo), enquanto roles do novo modelo são "Admin"/"Cliente". Não alterado —
   é decisão de wiring RBAC de outra task.
