# Task 7: Login, Register, GetCurrentUser com novo esquema (fecha a quebra de build)

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api.

**Objetivo:** reescrever os 3 handlers de auth (Login, RegisterUser, GetCurrentUser) para o novo modelo (sem `Usuario.Role`, com roles via tabela, access+refresh no login). **Ao fim desta task a solução inteira DEVE compilar** (fecha a quebra iniciada na Task 1).

## Constraints globais
- net10.0, noBOM, 1 classe/record por arquivo (os records de DTO podem coexistir no mesmo arquivo seguindo o padrão atual do `LoginResponse.cs`, que tem 2 records).
- DI manual em `ApplicationServiceCollectionExtensions` (sem scanning).
- Entidades (T1), interfaces (T2), services (T3), configs (T4), repositórios/resolver (T5) já existem.
- Interfaces relevantes:
  - `IJwtTokenGenerator.Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissoes)` (T2/T3).
  - `IRefreshTokenGenerator.Generate() → (string PlainToken, string Hash)` (T2/T3).
  - `IRefreshTokenStore.AddAsync(RefreshToken)` (T5).
  - `IPermissionResolver.ResolveAsync(Guid usuarioId, ct) → (IReadOnlyList<string> Roles, IReadOnlyList<string> Permissoes)` (T5).
  - `IUsuarioRepository`: `GetByEmailAsync`, `GetByIdAsync`, `GetByIdComRolesAsync`, `AddAsync`, `SetRolesAsync`, `SaveChangesAsync` (T5).
  - `IRoleRepository.GetByNomeAsync(string)` (T5) — para achar a role "Cliente" no registro.
  - `IClock.UtcNow`.
- Config de expiração do refresh: `Jwt:RefreshTokenDays` (default 7).

## Mudanças de contrato (DTOs)

`Login/LoginResponse.cs` — NOVO shape:
```csharp
namespace GatumsGourmet.Application.UseCases.V1.Auth.Login;
public record LoginUserDto(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles);
public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, LoginUserDto User);
```

`GetCurrentUser/GetCurrentUserResponse.cs` — trocar `string Role` por roles:
```csharp
public record GetCurrentUserResponse(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles);
```

`RegisterUser/RegisterUserResponse.cs` — trocar `string Role` por roles:
```csharp
public record RegisterUserResponse(Guid Id, string Nome, string Email, IReadOnlyList<string> Roles);
```

## Handlers

### LoginHandler (reescrever)
Dependências: `IUsuarioRepository usuarios, IPasswordHasher hasher, IJwtTokenGenerator jwt, IRefreshTokenGenerator refreshGen, IRefreshTokenStore refreshStore, IPermissionResolver permissions, IClock clock, IConfiguration config`.
Fluxo:
1. `usuario = await usuarios.GetByEmailAsync(request.Email, ct)`.
2. Se `usuario is null || !hasher.Verify(request.Senha, usuario.SenhaHash)` → `Fail("Credenciais inválidas.")`.
3. **Se `!usuario.Ativo`** → `Fail("Usuário inativo.")` (REVIEW FOCUS — usuário desativado não loga).
4. `(roles, permissoes) = await permissions.ResolveAsync(usuario.Id, ct)`.
5. `accessToken = jwt.Generate(usuario.Id, usuario.Email, roles, permissoes)`.
6. `(plain, hash) = refreshGen.Generate()`. Monta `RefreshToken { Id=Guid.NewGuid(), UsuarioId=usuario.Id, TokenHash=hash, FamiliaId=Guid.NewGuid(), ExpiraEm=clock.UtcNow.AddDays(refreshDays), CriadoEm=clock.UtcNow, Revogado=false }` (refreshDays de `Jwt:RefreshTokenDays`, default 7). `await refreshStore.AddAsync(token, ct); await refreshStore.SaveChangesAsync(ct);`
7. `expiresIn` = `Jwt:AccessTokenMinutes` (default 15) × 60 (segundos).
8. Retorna `Ok(new LoginResponse(accessToken, plain, expiresIn, new LoginUserDto(usuario.Id, usuario.Nome, usuario.Email, roles)))`.
A assinatura de `ILoginHandler.Handle(LoginRequest, ct)` permanece. `LoginRequest(Email, Senha)` permanece.

### RegisterUserHandler (reescrever)
Dependências: `IUsuarioRepository usuarios, IRoleRepository roles, IPasswordHasher hasher, IClock clock`.
Fluxo:
1. Se email já existe → `Fail("E-mail já cadastrado.")` (mantém).
2. Cria `Usuario { Id=Guid.NewGuid(), Nome, Email, Telefone, SenhaHash=hasher.Hash(Senha), Ativo=true, CriadoEm=clock.UtcNow }`.
3. `await usuarios.AddAsync(usuario, ct); await usuarios.SaveChangesAsync(ct);`
4. Vincular a role Cliente: `var clienteRole = await roles.GetByNomeAsync("Cliente", ct);` se não-nulo, `await usuarios.SetRolesAsync(usuario.Id, new[]{ clienteRole.Id }, ct); await usuarios.SaveChangesAsync(ct);`
5. Retorna `Ok(new RegisterUserResponse(usuario.Id, usuario.Nome, usuario.Email, new[]{"Cliente"}))`.

### GetCurrentUserHandler (reescrever)
Dependências: `IUsuarioRepository usuarios`.
Fluxo:
1. `usuario = await usuarios.GetByIdComRolesAsync(userId, ct)`.
2. Se null → `Fail("Usuário não encontrado.")`.
3. `roles = usuario.UsuarioRoles.Select(ur => ur.Role.Nome).ToList()`.
4. Retorna `Ok(new GetCurrentUserResponse(usuario.Id, usuario.Nome, usuario.Email, roles))`.

## DI (`ApplicationServiceCollectionExtensions.cs`)
Os registros de ILoginHandler/IRegisterUserHandler/IGetCurrentUserHandler já existem; como as dependências dos handlers mudaram (mais serviços injetados), isso é resolvido automaticamente pelo DI desde que os serviços estejam registrados (Infra já registra IJwtTokenGenerator, IRefreshTokenGenerator, IRefreshTokenStore, IPermissionResolver, IRoleRepository, IPasswordHasher, IClock). Verifique que não falta registro. Não precisa mudar as linhas de registro dos handlers.

## Testes (ajustar os existentes + novos) em `tests/.../V1/Auth/Login/LoginHandlerTests.cs`
O teste atual usa `UserRole`, `jwt.Generate(id,email,"admin")` e `LoginResponse.Token`/`.User.Role` — tudo isso MUDOU. Reescreva:
- Helper `CreateUsuario` sem `Role` (usa `Ativo = true` por default); roles agora vêm do `IPermissionResolver` mockado.
- Mockar `IPermissionResolver.ResolveAsync` retornando `(["Admin"], ["x:y"])`; `IRefreshTokenGenerator.Generate()` retornando `("plain","hash")`; `IJwtTokenGenerator.Generate(id,email,It.IsAny<IEnumerable<string>>(),It.IsAny<IEnumerable<string>>())` retornando "jwt"; `IClock.UtcNow`; `IConfiguration` em memória com `Jwt:AccessTokenMinutes=15`, `Jwt:RefreshTokenDays=7`.
- Testes:
  1. `CredenciaisValidas_RetornaAccessERefresh`: sucesso, `AccessToken=="jwt"`, `RefreshToken=="plain"`, `ExpiresIn==900`, `User.Roles` contém "Admin", e `refreshStore.AddAsync` foi chamado.
  2. `UsuarioInexistente_RetornaErro`.
  3. `SenhaIncorreta_RetornaErro`.
  4. **`UsuarioInativo_RetornaErro`** (REVIEW FOCUS): usuário com `Ativo=false`, senha correta → `Fail` e `refreshStore.AddAsync` NÃO chamado.

Siga TDD: ajuste os testes primeiro (vão falhar/ nem compilar), depois os handlers, depois rode.

## Validação FINAL (esta task fecha a quebra)
1. `dotnet build GatumsGourmet.sln` → **0 erros** (solução inteira volta a compilar).
2. `dotnet test tests/GatumsGourmet.UnitTests` → todos passam, incluindo os testes das Tasks 1/3 que não rodavam (RefreshTokenTests, JwtTokenGeneratorTests, RefreshTokenGeneratorTests) e os de Login reescritos. Reporte a contagem.

## Commit
`feat: login com access+refresh e register vinculado a role`

## Não dispare subagentes. Esta é a task que fecha a compilação — garanta build 0 erros e testes unitários verdes.
