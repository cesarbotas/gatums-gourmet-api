using FluentAssertions;
using GatumsGourmet.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Api.Authorization;

/// <summary>
/// Testes de unidade puros do policy provider dinâmico.
/// Não usam a collection "Integration" (não sobem container).
/// </summary>
public class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider BuildProvider(Action<AuthorizationOptions>? configure = null)
    {
        var options = new AuthorizationOptions();
        configure?.Invoke(options);
        return new PermissionPolicyProvider(Options.Create(options));
    }

    [Fact]
    public async Task GetPolicyAsync_ComPrefixoPerm_CriaPolicyComRequirement()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync("perm:x:y");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>()
            .Should().ContainSingle()
            .Which.Codigo.Should().Be("x:y");
        // RequireAuthenticatedUser adiciona um DenyAnonymousAuthorizationRequirement.
        policy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>()
            .Should().ContainSingle();
    }

    [Fact]
    public async Task GetPolicyAsync_NomeNaoPrefixado_DelegaAoFallback()
    {
        var provider = BuildProvider(opt =>
            opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

        var policy = await provider.GetPolicyAsync("AdminOnly");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should().BeEmpty();
    }

    [Fact]
    public async Task GetPolicyAsync_NomeDesconhecidoSemPrefixo_DelegaAoFallbackRetornandoNull()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync("Inexistente");

        policy.Should().BeNull();
    }
}
