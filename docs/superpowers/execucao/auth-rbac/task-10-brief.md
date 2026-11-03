# Task 10: Autorização por permissão dinâmica (policy provider)

**Projeto:** API .NET 10 (Clean Architecture), branch feature/AjusteLoginToken, em c:\Git\Botas\gatums-gourmet-api. Build 0 erros; 127 unit + 72 integração verdes.

**Objetivo:** criar o mecanismo de autorização por permissão — `RequirePermission("codigo")` nos endpoints, suportado por um `IAuthorizationPolicyProvider` dinâmico que cria policies sob demanda e um `AuthorizationHandler` que valida a claim `perm`.

## Constraints globais
- net10.0, noBOM, 1 classe por arquivo em `Api/Authorization/`.
- O access token traz uma claim `perm` por permissão (Task 3). O `HasPermission` (Task 9) já checa claim type "perm".

## Como funciona (padrão ASP.NET Core de policy dinâmica)
`RequirePermission("produtos:escrever")` adiciona ao endpoint uma policy nomeada `perm:produtos:escrever`. Um `PermissionPolicyProvider` (IAuthorizationPolicyProvider) intercepta nomes com prefixo `perm:`, cria a policy on-the-fly com um `PermissionRequirement(codigo)`, e um `PermissionAuthorizationHandler` aprova se o usuário tem a claim `perm == codigo`. Assim não é preciso registrar cada permissão no startup.

## Arquivos a criar em `Api/Authorization/` (1 por arquivo)

### `PermissionRequirement.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
namespace GatumsGourmet.Api.Authorization;
public sealed class PermissionRequirement(string codigo) : IAuthorizationRequirement
{
    public string Codigo { get; } = codigo;
}
```

### `PermissionAuthorizationHandler.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
namespace GatumsGourmet.Api.Authorization;
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Claims.Any(c => c.Type == "perm" && c.Value == requirement.Codigo))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

### `PermissionPolicyProvider.cs`
IAuthorizationPolicyProvider que:
- Tem o prefixo `const string Prefix = "perm:";`.
- Delega para um `DefaultAuthorizationPolicyProvider` fallback (construído com as `AuthorizationOptions`) para `GetDefaultPolicyAsync` e `GetFallbackPolicyAsync`.
- Em `GetPolicyAsync(policyName)`: se `policyName.StartsWith(Prefix, StringComparison.Ordinal)`, extrai `codigo = policyName[Prefix.Length..]`, cria e retorna `Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(codigo)).Build())`. Senão, delega ao fallback provider.
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
namespace GatumsGourmet.Api.Authorization;
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public const string Prefix = "perm:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) => _fallback = new DefaultAuthorizationPolicyProvider(options);
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var codigo = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(codigo))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}
```

### `RequirePermissionExtensions.cs`
```csharp
using Microsoft.AspNetCore.Builder;
namespace GatumsGourmet.Api.Authorization;
public static class RequirePermissionExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string codigo)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization($"{PermissionPolicyProvider.Prefix}{codigo}");
}
```

## Registro no DI (`AuthenticationExtensions.cs`)
No `AddGatumsAuthentication`, após `AddAuthorization`:
- `services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();`
- `services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();`
(usings: `GatumsGourmet.Api.Authorization;`, `Microsoft.AspNetCore.Authorization;`)
MANTER as policies legadas "AdminOnly"/"ClienteOnly" por enquanto (a Task 11 as remove ao migrar as rotas). Não remover nesta task para não quebrar os endpoints que ainda as usam.

## Teste de integração `tests/.../V1/Auth/PermissionPolicyTests.cs`
`[Collection("Integration")]`, `CustomWebApplicationFactory`.
Para testar sem depender de rotas reais ainda-não-migradas, o teste pode usar um endpoint existente protegido por permissão? Ainda não há — as rotas migram na Task 11. ENTÃO: crie o teste exercitando o mecanismo de forma end-to-end mínima. Abordagem recomendada: a factory permite customizar o host? Em vez de inventar endpoint, teste via um usuário com/sem a permissão batendo numa rota que SERÁ protegida — mas ela ainda não está. 
Alternativa pragmática e suficiente: teste de UNIDADE do `PermissionAuthorizationHandler` e do `PermissionPolicyProvider` (não precisa de container):
- `tests/GatumsGourmet.UnitTests/Api/Authorization/PermissionAuthorizationHandlerTests.cs`:
  1. Usuário COM claim `perm=produtos:escrever` → `context.HasSucceeded == true`.
  2. **Usuário SEM a claim → `context.HasSucceeded == false`** (REVIEW FOCUS: resultará em 403, não 500). Monte o `AuthorizationHandlerContext` com um ClaimsPrincipal (ClaimsIdentity autenticada) com/sem a claim "perm".
- `PermissionPolicyProviderTests.cs`:
  1. `GetPolicyAsync("perm:x:y")` retorna policy não-nula contendo um PermissionRequirement com Codigo "x:y" e RequireAuthenticatedUser.
  2. `GetPolicyAsync("AdminOnly")` delega ao fallback (retorna a policy registrada ou null coerente).
(O projeto de testes de unidade precisa referenciar o Api? Verifique — se não referenciar, adicione a ProjectReference do Api ao UnitTests.csproj, ou coloque esses testes no projeto de integração que já referencia o Api. PREFIRA colocar no projeto que já referencia o Api para evitar nova ProjectReference; se for o IntegrationTests, não precisa de container para esses testes de unidade do handler — eles são puros. Decida e justifique.)

## Validação
`dotnet build GatumsGourmet.sln` 0 erros; rode a suíte onde os testes foram colocados (unit e/ou integração) e reporte contagem. Como as policies legadas foram mantidas, nenhum endpoint existente quebra.

## Commit
`feat: autorizacao por permissao via policy provider dinamico`

## Não dispare subagentes. Mantenha as policies legadas (Task 11 remove). O teste "sem permissão → não autoriza" é o Review Focus.
