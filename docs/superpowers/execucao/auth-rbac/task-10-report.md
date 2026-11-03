# Task 10 — Relatório: Autorização por permissão dinâmica (policy provider)

## Status
Concluída. Build 0 erros; mecanismo de autorização por permissão criado e registrado no DI. Policies legadas "AdminOnly"/"ClienteOnly" mantidas (a Task 11 as remove ao migrar rotas).

## Classes criadas (em `src/GatumsGourmet.Api/Authorization/`, 1 por arquivo, noBOM)
- `PermissionRequirement.cs` — `IAuthorizationRequirement` com propriedade `Codigo`.
- `PermissionAuthorizationHandler.cs` — `AuthorizationHandler<PermissionRequirement>`; aprova se existe claim `perm == Codigo`. Ausência não falha explicitamente → resulta em 403 (não 500).
- `PermissionPolicyProvider.cs` — `IAuthorizationPolicyProvider`; prefixo `perm:`; cria policy on-the-fly (`RequireAuthenticatedUser` + `PermissionRequirement`) para nomes prefixados; delega os demais ao `DefaultAuthorizationPolicyProvider` (fallback), preservando "AdminOnly"/"ClienteOnly".
- `RequirePermissionExtensions.cs` — `RequirePermission<TBuilder>(codigo)` → `RequireAuthorization("perm:{codigo}")` para endpoints.

## Registro no DI (`Extensions/AuthenticationExtensions.cs`)
Após `AddAuthorization` (policies legadas mantidas):
- `services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();`
- `services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();`
Usings adicionados: `GatumsGourmet.Api.Authorization;`, `Microsoft.AspNetCore.Authorization;`.

## Onde coloquei os testes e por quê
`tests/GatumsGourmet.IntegrationTests/Api/Authorization/` (PermissionAuthorizationHandlerTests.cs, PermissionPolicyProviderTests.cs).
Justificativa: o projeto IntegrationTests **já referencia** o `GatumsGourmet.Api` (`ProjectReference`) e tem `FrameworkReference Microsoft.AspNetCore.App` (necessário para `IAuthorizationPolicyProvider`/`AuthorizationPolicyBuilder`). O projeto UnitTests **não** referencia o Api. Conforme preferência do brief, evitei adicionar nova `ProjectReference` ao UnitTests. Os testes são **puros** (montam `AuthorizationHandlerContext`/`ClaimsPrincipal` em memória, instanciam o provider com `Options.Create(new AuthorizationOptions())`): **não** usam `[Collection("Integration")]`, portanto **não** sobem o container PostgreSQL.

## Evidência TDD (RED → GREEN)
- RED: com os testes escritos e sem as classes, `dotnet build` falhou com CS0234/CS0246 ("namespace Authorization não existe", "PermissionPolicyProvider não encontrado").
- Após criar as 4 classes: 1 erro restante **só no teste** (CS0246 `DenyAnonymousAuthorizationRequirement`) — faltava `using Microsoft.AspNetCore.Authorization.Infrastructure;`. Corrigido no teste.
- GREEN: build com êxito, testes passando.

## Build + Testes (contagem)
- `dotnet build GatumsGourmet.sln`: **0 erros** ("Compilação com êxito"). Warnings pré-existentes (NU1902/NU1903 em System.Security.Cryptography.Pkcs; MSB3277 conflito de versão EFCore.Relational; CS0105 usings duplicados em testes antigos) — nenhum introduzido por esta task.
- Testes de autorização (filtro `FullyQualifiedName~Api.Authorization`, `--no-build`): **6 aprovados, 0 falhas** (3 handler + 3 provider), 70 ms. Não subiu container.
- Suíte unit completa (UnitTests): **127 aprovados, 0 falhas**, 754 ms (sem regressão).
- Integração completa (72 testes) exige Docker/Postgres e envolve rotas ainda não migradas (Task 11); como as policies legadas foram mantidas, endpoints existentes não quebram. O mecanismo desta task foi validado diretamente pelos 6 testes puros.

## Casos de teste
- Handler: com claim `perm` correta → `HasSucceeded == true`; **sem a claim** (usuário autenticado) → `HasSucceeded == false` (REVIEW FOCUS); com `perm` de outro código → `false`.
- Provider: `perm:x:y` → policy não-nula com `PermissionRequirement(Codigo="x:y")` + `DenyAnonymousAuthorizationRequirement` (RequireAuthenticatedUser); `AdminOnly` → delega ao fallback (policy sem `PermissionRequirement`); nome desconhecido sem prefixo → `null`.

## Arquivos alterados/criados
- Criados: `src/GatumsGourmet.Api/Authorization/{PermissionRequirement,PermissionAuthorizationHandler,PermissionPolicyProvider,RequirePermissionExtensions}.cs`.
- Alterado: `src/GatumsGourmet.Api/Extensions/AuthenticationExtensions.cs` (usings + 2 registros DI; policies legadas mantidas).
- Testes: `tests/GatumsGourmet.IntegrationTests/Api/Authorization/{PermissionAuthorizationHandlerTests,PermissionPolicyProviderTests}.cs`.

## Auto-revisão do diff
- `PermissionPolicyProvider` é `Singleton` (sem estado mutável; `DefaultAuthorizationPolicyProvider` construído uma vez) — correto e thread-safe. Handler `Scoped` conforme padrão ASP.NET Core.
- Prefixo centralizado em `PermissionPolicyProvider.Prefix`, reutilizado por `RequirePermissionExtensions` — sem string mágica duplicada.
- `StringComparison.Ordinal` no `StartsWith` — comparação exata, sem cultura.
- Fallback para nomes não prefixados preserva "AdminOnly"/"ClienteOnly" e policies default/fallback.
- Claim type `perm` consistente com `ClaimsPrincipalExtensions.HasPermission` (Task 9) e com a emissão do token (Task 3).

## Concerns
- Nenhum teste end-to-end via HTTP nesta task (não há rota protegida por permissão até a Task 11). A validação é por testes puros do handler/provider. A Task 11 deve adicionar ao menos um teste de integração batendo numa rota migrada com/sem a permissão (401 vs 403 vs 200).
- Warnings pré-existentes de vulnerabilidade (Pkcs) e conflito de versão EFCore.Relational permanecem fora do escopo desta task.
