using Microsoft.AspNetCore.Builder;

namespace GatumsGourmet.Api.Authorization;

/// <summary>
/// Açúcar sintático para proteger endpoints por permissão. Adiciona a policy
/// nomeada <c>perm:{codigo}</c>, resolvida dinamicamente pelo
/// <see cref="PermissionPolicyProvider"/>.
/// </summary>
public static class RequirePermissionExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string codigo)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization($"{PermissionPolicyProvider.Prefix}{codigo}");
}
