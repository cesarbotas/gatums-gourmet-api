using Microsoft.AspNetCore.Authorization;

namespace GatumsGourmet.Api.Authorization;

/// <summary>
/// Aprova o requisito quando o usuário possui a claim <c>perm</c> com o código
/// exigido. Caso contrário não falha explicitamente: a ausência resulta em 403.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Claims.Any(c => c.Type == "perm" && c.Value == requirement.Codigo))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
