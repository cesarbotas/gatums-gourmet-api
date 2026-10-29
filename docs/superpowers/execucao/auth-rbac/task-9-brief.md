# Task 9: Endpoints de Auth (v1) — refresh/logout/logout-all + HasPermission + teste de fluxo

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 127 unit + 71 integração verdes.

**Objetivo:** expor os endpoints de auth novos (refresh público, logout, logout-all), adicionar o helper `HasPermission` em ClaimsPrincipalExtensions, e um teste de integração do fluxo completo (login → me → refresh → reúso → logout).

## Constraints globais
- net10.0, noBOM. Endpoints em `Api/Endpoints/V1/AuthEndpoints.cs` (modificar o existente).
- ALIAS: dentro de `...V1.Auth.*` o namespace `RefreshToken` sombreia a entidade. No endpoint, importe os handlers por namespace de use case; se precisar referenciar a entidade, use alias (provável que NÃO precise aqui — o endpoint só usa Request/Handler).
- Padrão dos endpoints existentes (Minimal API, `group.MapPost(...)`, `.ToHttpResult()`, `RequireAuthorization()`), ver AuthEndpoints atual abaixo.

## Estado atual de `AuthEndpoints.cs`
```csharp
using System.Security.Claims;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;
using GatumsGourmet.Application.UseCases.V1.Auth.Login;
using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class AuthEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/login", async ([FromBody] LoginRequest request, ILoginHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult());

        group.MapPost("/register", async ([FromBody] RegisterUserRequest request, IRegisterUserHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created));

        group.MapGet("/me", async (ClaimsPrincipal user, IGetCurrentUserHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/logout", (ClaimsPrincipal _) => Results.Ok())
            .RequireAuthorization();
    }
}
```

## Trabalho

### 1. Adicionar `HasPermission` em `Api/Extensions/ClaimsPrincipalExtensions.cs`
Manter `GetUserId` e `IsAdmin`. Adicionar:
```csharp
public static bool HasPermission(this ClaimsPrincipal user, string codigo) =>
    user.Claims.Any(c => c.Type == "perm" && c.Value == codigo);
```

### 2. Reescrever os endpoints de auth em `AuthEndpoints.cs`
Importar os namespaces dos use cases: `...V1.Auth.Login`, `...RegisterUser`, `...GetCurrentUser`, `...RefreshToken`, `...Logout`, `...LogoutAll`.
Atenção ao sombreamento: `using GatumsGourmet.Application.UseCases.V1.Auth.RefreshToken;` traz o namespace do use case. O tipo `RefreshTokenRequest`/`IRefreshTokenHandler` resolvem por esse using. Isso é OK no endpoint (não referencia a entidade RefreshToken).

Rotas (grupo é `/api/v1/auth`):
- `POST /login` — mantém (público). Mesmo handler.
- `POST /register` — mantém (público). Mesmo handler.
- `POST /refresh` — PÚBLICO (sem RequireAuthorization). Recebe `[FromBody] RefreshTokenRequest request`, `IRefreshTokenHandler handler`, `HttpContext http`, `CancellationToken ct`. Extrai o IP: `var ip = http.Connection.RemoteIpAddress?.ToString();` (pode usar `X-Forwarded-For` se presente: `http.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? http.Connection.RemoteIpAddress?.ToString()`). Chama `handler.Handle(request, ip, ct)` e `.ToHttpResult()`.
- `GET /me` — mantém (RequireAuthorization).
- `POST /logout` — REESCREVER: agora recebe `[FromBody] LogoutRequest request, ClaimsPrincipal user, ILogoutHandler handler, CancellationToken ct`, chama `handler.Handle(user.GetUserId(), request, ct)` e `.ToHttpResult()`. RequireAuthorization().
- `POST /logout-all` — NOVO: `ClaimsPrincipal user, ILogoutAllHandler handler, CancellationToken ct` → `handler.Handle(user.GetUserId(), ct)` → `.ToHttpResult()`. RequireAuthorization().

### 3. Teste de integração do fluxo (`tests/.../V1/Auth/AuthFlowEndpointTests.cs`)
`[Collection("Integration")]`, recebe `CustomWebApplicationFactory`. Fluxo (usa o cliente HTTP real; registra um usuário novo via POST /register para ter credenciais conhecidas, OU usa a AuthenticatedClientFactory — mas para refresh precisa do refreshToken do login, então faça login manual):
1. `POST /api/v1/auth/register` com email único + senha → 201.
2. `POST /api/v1/auth/login` com as credenciais → 200; captura `accessToken` e `refreshToken` do corpo.
3. `GET /api/v1/auth/me` com Bearer accessToken → 200; confirma o email.
4. `POST /api/v1/auth/refresh` com `{ refreshToken }` → 200; captura `novoRefresh` (diferente do original) e novo accessToken.
5. **Reúso:** `POST /api/v1/auth/refresh` com o refreshToken ORIGINAL (já rotacionado) → 401/400 (Fail). (Confirma detecção de reúso.)
6. **Família revogada:** `POST /api/v1/auth/refresh` com o `novoRefresh` (que pertencia à mesma família) → também deve falhar (a detecção de reúso no passo 5 revogou a família inteira). Isto CONFIRMA a persistência da revogação de família (pointer da Task 8).
7. `POST /api/v1/auth/logout` com Bearer + um refreshToken → 200.

Ajuste os status esperados ao que o ResultExtensions mapeia para Fail (verifique: provavelmente 400 BadRequest). Asserte o comportamento, não um código específico se não tiver certeza — mas confirme que reúso e família revogada NÃO retornam 200 com token.

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; `dotnet test tests/GatumsGourmet.IntegrationTests` (container de pé) — AuthFlowEndpointTests passa; rode a suíte inteira e reporte contagem; investigue regressões.

## Commit
`feat: endpoints de auth com refresh/logout/logout-all`

## Não dispare subagentes. O teste do passo 6 (família revogada após reúso) é importante — confirma a persistência da revogação.
