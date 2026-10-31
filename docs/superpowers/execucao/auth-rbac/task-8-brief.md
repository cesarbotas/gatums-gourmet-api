# Task 8: RefreshToken, Logout, LogoutAll (use cases) — rotação com detecção de reúso

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. A solução compila (build 0 erros; 119 unit + 71 integração verdes).

**Objetivo:** criar os use cases de renovação (refresh rotativo com detecção de reúso por família), logout (revoga 1 refresh) e logout-all (revoga todos do usuário). É o ponto mais sensível de segurança — siga o fluxo exato.

## Constraints globais
- net10.0, noBOM, 1 classe por arquivo. Use cases em `Application/UseCases/V1/Auth/{RefreshToken,Logout,LogoutAll}/`, cada um com Handler + Interface + Request + Response quando aplicável.
- DI manual em `ApplicationServiceCollectionExtensions` — registrar os 3 handlers novos.
- Reusar o `LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, LoginUserDto User)` e `LoginUserDto(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles)` do namespace `...V1.Auth.Login` (NÃO duplicar; o refresh retorna o mesmo shape).
- Interfaces disponíveis (Tasks 2/5): `IRefreshTokenStore { AddAsync; GetByHashAsync(hash); RevokeFamilyAsync(familiaId, motivo, em); RevokeAllForUserAsync(usuarioId, motivo, em); DeleteExpiredAsync; SaveChangesAsync }`, `IRefreshTokenGenerator { Generate(); Hash(plain) }`, `IJwtTokenGenerator.Generate(userId,email,roles,permissoes)`, `IPermissionResolver.ResolveAsync(usuarioId)`, `IUsuarioRepository.GetByIdAsync`, `IClock`, `IConfiguration`.
- Entidade `RefreshToken { Id; UsuarioId; TokenHash; FamiliaId; ExpiraEm; CriadoEm; Revogado; RevogadoEm?; SubstituidoPorTokenHash?; MotivoRevogacao?; CriadoPorIp? }`.

## RefreshToken use case (`Application/UseCases/V1/Auth/RefreshToken/`)

`RefreshTokenRequest(string RefreshToken)`.
`IRefreshTokenHandler { Task<Result<LoginResponse>> Handle(RefreshTokenRequest request, string? ip, CancellationToken ct = default); }`

`RefreshTokenHandler` — dependências: `IRefreshTokenStore store, IRefreshTokenGenerator gen, IJwtTokenGenerator jwt, IPermissionResolver permissions, IUsuarioRepository usuarios, IClock clock, IConfiguration config`.

Fluxo (ordem importa):
1. `hash = gen.Hash(request.RefreshToken)`.
2. `token = await store.GetByHashAsync(hash, ct)`.
3. Se `token is null` → `Fail("Refresh token inválido.")`.
4. **Detecção de reúso:** se `token.Revogado` → o token já foi rotacionado/revogado e está sendo reapresentado = sinal de roubo. `await store.RevokeFamilyAsync(token.FamiliaId, "Reúso detectado", clock.UtcNow, ct)` e `Fail("Refresh token inválido.")`. (REVIEW FOCUS)
5. Se `token.ExpiraEm <= clock.UtcNow` → `Fail("Refresh token expirado.")`. (REVIEW FOCUS)
6. `usuario = await usuarios.GetByIdAsync(token.UsuarioId, ct)`. Se null → `Fail("Refresh token inválido.")`.
7. **Se `!usuario.Ativo`** → `Fail("Usuário inativo.")`. (REVIEW FOCUS)
8. **Rotação:**
   - `(novoPlain, novoHash) = gen.Generate()`.
   - Marca o token atual: `token.Revogado = true; token.RevogadoEm = clock.UtcNow; token.SubstituidoPorTokenHash = novoHash; token.MotivoRevogacao = "Rotacionado";`
   - Cria o novo refresh preservando a família: `new RefreshToken { Id=Guid.NewGuid(), UsuarioId=token.UsuarioId, TokenHash=novoHash, FamiliaId=token.FamiliaId, ExpiraEm=clock.UtcNow.AddDays(refreshDays), CriadoEm=clock.UtcNow, Revogado=false, CriadoPorIp=ip }`.
   - `await store.AddAsync(novo, ct); await store.SaveChangesAsync(ct);` (o update do token atual é persistido no mesmo SaveChanges, pois GetByHashAsync é tracked).
9. Recalcula permissões: `(roles, permissoes) = await permissions.ResolveAsync(usuario.Id, ct)`; `accessToken = jwt.Generate(usuario.Id, usuario.Email, roles, permissoes)`.
10. `expiresIn = accessMinutes*60`. Retorna `Ok(new LoginResponse(accessToken, novoPlain, expiresIn, new LoginUserDto(usuario.Id, usuario.Nome, usuario.Email, roles)))`.
`refreshDays` de `Jwt:RefreshTokenDays` (default 7); `accessMinutes` de `Jwt:AccessTokenMinutes` (default 15).

## Logout use case (`Application/UseCases/V1/Auth/Logout/`)
`LogoutRequest(string RefreshToken)`.
`ILogoutHandler { Task<Result<bool>> Handle(Guid usuarioId, LogoutRequest request, CancellationToken ct = default); }`
`LogoutHandler` — deps: `IRefreshTokenStore store, IRefreshTokenGenerator gen, IClock clock`.
Fluxo: `hash = gen.Hash(request.RefreshToken)`; `token = await store.GetByHashAsync(hash, ct)`. Se token existe E `token.UsuarioId == usuarioId` E `!token.Revogado` → revoga a família dele (`store.RevokeFamilyAsync(token.FamiliaId, "Logout", clock.UtcNow, ct)`). Sempre retorna `Ok(true)` (logout é idempotente; não vaza se o token existe). NÃO revelar se o token era válido.

## LogoutAll use case (`Application/UseCases/V1/Auth/LogoutAll/`)
`ILogoutAllHandler { Task<Result<bool>> Handle(Guid usuarioId, CancellationToken ct = default); }`
`LogoutAllHandler` — deps: `IRefreshTokenStore store, IClock clock`.
Fluxo: `await store.RevokeAllForUserAsync(usuarioId, "LogoutAll", clock.UtcNow, ct); return Ok(true);`

## DI
Registrar em `ApplicationServiceCollectionExtensions`: `IRefreshTokenHandler→RefreshTokenHandler`, `ILogoutHandler→LogoutHandler`, `ILogoutAllHandler→LogoutAllHandler`.

## Testes unitários (`tests/.../V1/Auth/RefreshToken/RefreshTokenHandlerTests.cs`)
Mockar store, gen, jwt, permissions, usuarios, clock, config (InMemory com Jwt:RefreshTokenDays=7, Jwt:AccessTokenMinutes=15). Usuario de teste com Ativo=true.
Testes:
1. `RefreshValido_RotacionaEEmiteNovos`: token válido (Revogado=false, ExpiraEm futuro), usuário ativo → sucesso; AccessToken e RefreshToken não vazios; o token antigo foi marcado Revogado com SubstituidoPorTokenHash; `store.AddAsync` chamado (novo token); permissões recalculadas (ResolveAsync chamado).
2. **`RefreshJaRevogado_RevogaFamiliaEFalha`** (REVIEW FOCUS): token com Revogado=true → `Fail` E `store.RevokeFamilyAsync(familiaId,...)` chamado uma vez; `store.AddAsync` NÃO chamado.
3. **`RefreshExpirado_Falha`** (REVIEW FOCUS): token Revogado=false mas ExpiraEm no passado → `Fail("Refresh token expirado.")`; sem rotação.
4. **`UsuarioInativo_Falha`** (REVIEW FOCUS): token válido mas usuario.Ativo=false → `Fail`; sem rotação.
5. `TokenInexistente_Falha`: GetByHashAsync retorna null → `Fail`.

(Opcional, se simples: um teste de LogoutHandler verificando que RevokeFamilyAsync é chamado quando o token pertence ao usuário.)

Siga TDD: testes primeiro, falham, implementa, passam.

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test tests/GatumsGourmet.UnitTests` todos verdes (reporte contagem). Os endpoints que expõem esses handlers vêm na Task 9 — aqui são só os use cases.

## Commit
`feat: refresh rotativo com deteccao de reuso, logout e logout-all`

## Não dispare subagentes. A detecção de reúso (revogar família ao ver token revogado reapresentado) é o ponto crítico — não simplifique.
