using System.Security.Claims;
using FluentAssertions;
using GatumsGourmet.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Api.Authorization;

/// <summary>
/// Testes de unidade puros do handler de autorização por permissão.
/// Não usam a collection "Integration" (não sobem container): montam
/// manualmente o <see cref="AuthorizationHandlerContext"/>.
/// </summary>
public class PermissionAuthorizationHandlerTests
{
    private const string Codigo = "produtos:escrever";

    private static AuthorizationHandlerContext BuildContext(ClaimsPrincipal user)
    {
        var requirement = new PermissionRequirement(Codigo);
        return new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);
    }

    private static ClaimsPrincipal AuthenticatedUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task HandleRequirementAsync_ComClaimPerm_Aprova()
    {
        var user = AuthenticatedUser(new Claim("perm", Codigo));
        var context = BuildContext(user);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_SemClaimPerm_NaoAprova()
    {
        // REVIEW FOCUS: usuário autenticado mas sem a permissão -> 403, não 500.
        var user = AuthenticatedUser(new Claim(ClaimTypes.Name, "fulano"));
        var context = BuildContext(user);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_ComClaimPermDeOutroCodigo_NaoAprova()
    {
        var user = AuthenticatedUser(new Claim("perm", "pedidos:ler"));
        var context = BuildContext(user);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
