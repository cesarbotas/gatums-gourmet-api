# Task 8 — RefreshToken (rotação + detecção de reúso), Logout, LogoutAll

## Status
Concluída. Build 0 erros; 127 testes unitários verdes (119 baseline + 5 RefreshToken + 3 Logout).

## Use cases criados
Todos em `src/GatumsGourmet.Application/UseCases/V1/Auth/`, 1 classe por arquivo, net10.0, noBOM.

- `RefreshToken/` — `RefreshTokenRequest`, `IRefreshTokenHandler`, `RefreshTokenHandler`.
  - Fluxo (ordem exata do brief): `hash` → `GetByHashAsync` → null `Fail("Refresh token inválido.")` → **reúso** (se `Revogado`: `RevokeFamilyAsync(familiaId,"Reúso detectado",now)` + `Fail`) → expirado (`ExpiraEm <= now` → `Fail("Refresh token expirado.")`) → usuário null `Fail` → `!Ativo` `Fail("Usuário inativo.")` → rotação (marca atual `Revogado/RevogadoEm/SubstituidoPorTokenHash/MotivoRevogacao="Rotacionado"`; cria novo mesmo `FamiliaId`, `CriadoPorIp=ip`; `AddAsync` + `SaveChangesAsync`) → `ResolveAsync` → `jwt.Generate` → `Ok(LoginResponse)`.
  - Reusa `LoginResponse`/`LoginUserDto` de `...V1.Auth.Login` (não duplicado).
- `Logout/` — `LogoutRequest`, `ILogoutHandler`, `LogoutHandler`.
  - Só revoga família se token existe E `UsuarioId == usuarioId` E `!Revogado`. Sempre `Ok(true)` (idempotente, não vaza validade).
- `LogoutAll/` — `ILogoutAllHandler`, `LogoutAllHandler`.
  - `RevokeAllForUserAsync(usuarioId,"LogoutAll",now)` → `Ok(true)`.

## DI
Registrados em `ApplicationServiceCollectionExtensions`:
`IRefreshTokenHandler→RefreshTokenHandler`, `ILogoutHandler→LogoutHandler`, `ILogoutAllHandler→LogoutAllHandler` (Scoped).

## Evidência TDD RED/GREEN (RefreshTokenHandler)
- RED: stub do handler retornando `Fail("not implemented")` → `dotnet test --filter RefreshTokenHandlerTests` = **Com falha: 5, Aprovado: 0, Total: 5**.
- GREEN: implementação completa → **Aprovado: 5, Com falha: 0, Total: 5**.
- Testes: `RefreshValido_RotacionaEEmiteNovos`, `RefreshJaRevogado_RevogaFamiliaEFalha` (verifica `RevokeFamilyAsync` 1x e `AddAsync` nunca), `RefreshExpirado_Falha`, `UsuarioInativo_Falha`, `TokenInexistente_Falha`.
- Extra: `LogoutHandlerTests` (3) — revoga família do dono; não revoga token de outro usuário mas retorna Ok; token inexistente retorna Ok sem revogar.

## Build + testes
- `dotnet build GatumsGourmet.sln`: **0 Erro(s)** (14 warnings pré-existentes de unificação de versão do EF Core nos testes de integração; não introduzidos aqui).
- `dotnet test tests/GatumsGourmet.UnitTests`: **Aprovado: 127, Com falha: 0, Total: 127**.

## Arquivos alterados
Novos:
- `src/.../Auth/RefreshToken/{RefreshTokenRequest,IRefreshTokenHandler,RefreshTokenHandler}.cs`
- `src/.../Auth/Logout/{LogoutRequest,ILogoutHandler,LogoutHandler}.cs`
- `src/.../Auth/LogoutAll/{ILogoutAllHandler,LogoutAllHandler}.cs`
- `tests/.../Auth/RefreshToken/RefreshTokenHandlerTests.cs`
- `tests/.../Auth/Logout/LogoutHandlerTests.cs`

Modificados:
- `src/.../ApplicationServiceCollectionExtensions.cs` (usings + 3 registros DI).
- `src/.../Auth/Login/LoginHandler.cs` (alias `RefreshTokenEntity`).
- `tests/.../Auth/Login/LoginHandlerTests.cs` (alias `RefreshTokenEntity`).

## Auto-revisão
- Ordem das validações confere com o brief (reúso antes de expirado; usuário inativo antes de rotação).
- Reúso: `RevokeFamilyAsync` chamado com a `FamiliaId` do token revogado reapresentado; nenhum token novo gravado (`AddAsync`/`SaveChanges` só após todas as validações). Teste cobre explicitamente.
- Rotação: token antigo marcado (`Revogado=true`, `SubstituidoPorTokenHash`, `MotivoRevogacao="Rotacionado"`), novo criado na mesma família, persistidos no mesmo `SaveChangesAsync` (GetByHashAsync é tracked).
- Nenhum token gravado em caminho de falha (null/reúso/expirado/usuário null/inativo retornam antes do `AddAsync`).
- `LoginResponse`/`LoginUserDto` reusados, não duplicados.

## Concerns
- **Colisão de namespace:** criar a pasta/namespace `...V1.Auth.RefreshToken` passou a sombrear o tipo entidade `RefreshToken` em toda a subárvore `Auth`, quebrando `LoginHandler` e `LoginHandlerTests`. Resolvido com alias `using RefreshTokenEntity = GatumsGourmet.Domain.Entities.Auth.RefreshToken;` nesses arquivos. O novo `RefreshTokenHandler` usa o mesmo alias. Se a Task 9 adicionar mais referências à entidade dentro de `...V1.Auth.*`, usar o mesmo alias.
- Endpoints que expõem esses handlers ficam para a Task 9 (fora de escopo aqui).
