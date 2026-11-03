# Task 9 — Report: Endpoints de Auth (refresh/logout/logout-all) + HasPermission + teste de fluxo

## Status
Concluída. Build 0 erros, 72 testes de integração verdes (71 anteriores + 1 novo de fluxo). Commit `584575b`.

## Endpoints / helper

### `ClaimsPrincipalExtensions.cs`
Adicionado `HasPermission(this ClaimsPrincipal, string codigo)` que checa claims do tipo `"perm"` com valor igual ao código. `GetUserId` e `IsAdmin` mantidos intactos.

```csharp
public static bool HasPermission(this ClaimsPrincipal user, string codigo) =>
    user.Claims.Any(c => c.Type == "perm" && c.Value == codigo);
```

### `AuthEndpoints.cs` (grupo `/api/v1/auth`)
- `POST /login` — mantido, público.
- `POST /register` — mantido, público (201).
- `POST /refresh` — **NOVO, público** (sem `RequireAuthorization`). Recebe `RefreshTokenRequest`, `IRefreshTokenHandler`, `HttpContext`, `CancellationToken`. Extrai o IP preferindo `X-Forwarded-For`, com fallback para `RemoteIpAddress`. Chama `handler.Handle(request, ip, ct)` → `.ToHttpResult()`.
- `GET /me` — mantido, `RequireAuthorization`.
- `POST /logout` — **REESCRITO**: recebe `LogoutRequest`, `ClaimsPrincipal`, `ILogoutHandler`, chama `handler.Handle(user.GetUserId(), request, ct)` → `.ToHttpResult()`. `RequireAuthorization`.
- `POST /logout-all` — **NOVO**: `ClaimsPrincipal`, `ILogoutAllHandler` → `handler.Handle(user.GetUserId(), ct)` → `.ToHttpResult()`. `RequireAuthorization`.

Sombreamento de namespace: `using ...V1.Auth.RefreshToken;` traz o namespace do use case; o endpoint só referencia `RefreshTokenRequest`/`IRefreshTokenHandler`, então não precisou de alias para a entidade.

## Teste de fluxo — `AuthFlowEndpointTests.cs`
`[Collection("Integration")]`, usa `CustomWebApplicationFactory` (cliente HTTP real). Fluxo:
1. `POST /register` com email único → 201.
2. `POST /login` → 200; captura `accessToken` e `refreshToken` original.
3. `GET /me` com Bearer → 200; confirma o `email`.
4. `POST /refresh` com o refresh original → 200; captura `novoAccess` e `novoRefresh`; asserta `novoRefresh != refreshOriginal` (rotação ocorreu).
5. **Reúso:** `POST /refresh` com o refresh ORIGINAL (já rotacionado) → 400 BadRequest (detecção de reúso).
6. **Família revogada:** `POST /refresh` com o `novoRefresh` (mesma família) → 400 BadRequest.
7. `POST /logout` com Bearer `novoAccess` + `novoRefresh` no corpo → 200.

### O que o teste confirma
O passo 6 é o ponto crítico: quando o token original (passo 5) é reapresentado, o handler revoga a **família inteira** (`RevokeFamilyAsync`) e persiste via `SaveChangesAsync`. Como o `novoRefresh` pertence à mesma família, o passo 6 também falha — provando que a **revogação de família persiste** entre requisições HTTP independentes (pointer da Task 8). Status Fail → 400 confirmado via `ResultExtensions` (`Results.Problem(..., 400)`).

## Validação
- `dotnet build GatumsGourmet.sln` → **0 erros** (14 warnings pré-existentes: MSB3277 conflito de versão EFCore.Relational + NU1903 Pkcs; não introduzidos nesta task).
- `dotnet test tests/GatumsGourmet.IntegrationTests` → **Aprovado: 72, Falha: 0, Total: 72** (~1m28s). Container Postgres de teste (porta 5433) de pé e saudável. Sem regressões.

## Arquivos alterados
- `src/GatumsGourmet.Api/Extensions/ClaimsPrincipalExtensions.cs` (modificado)
- `src/GatumsGourmet.Api/Endpoints/V1/AuthEndpoints.cs` (modificado)
- `tests/GatumsGourmet.IntegrationTests/Application/UseCases/V1/Auth/AuthFlowEndpointTests.cs` (novo)

## Auto-revisão
- Diff revisado: 3 arquivos, +116/-2. Nenhuma alteração fora do escopo.
- noBOM verificado nos 3 arquivos (AuthEndpoints.cs havia nascido com BOM pela ferramenta de escrita; BOM removido antes do commit).
- Endpoints seguem o padrão existente (Minimal API, `.ToHttpResult()`, `RequireAuthorization()`).
- `/refresh` e `/login`/`/register` permanecem públicos; `/me`, `/logout`, `/logout-all` exigem autenticação.
- Teste asserta comportamento (reúso e família revogada não retornam 200 com token) e também o código concreto (400), coerente com o mapeamento Fail→BadRequest do `ResultExtensions`.

## Concerns
- **Warnings de build pré-existentes** (conflito EFCore.Relational 10.0.4 vs 10.0.13; vulnerabilidade System.Security.Cryptography.Pkcs 9.0.0). Fora do escopo desta task, mas vale endereçar em manutenção de dependências.
- `HasPermission` foi adicionado conforme o brief, mas ainda **não é consumido** por nenhum endpoint nesta task — fica disponível para autorização por permissão em tasks futuras.
- O teste depende do container Postgres de teste já em execução (o compose up/down automático está desativado na factory).

## Commit
`584575b` — feat: endpoints de auth com refresh/logout/logout-all
