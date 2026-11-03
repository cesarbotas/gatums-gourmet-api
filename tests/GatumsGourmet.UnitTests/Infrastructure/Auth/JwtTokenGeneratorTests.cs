using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using GatumsGourmet.Infrastructure.Services.Auth;
using Microsoft.Extensions.Configuration;

namespace GatumsGourmet.UnitTests.Infrastructure.Auth;

public class JwtTokenGeneratorTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-with-at-least-32-chars!!",
                ["Jwt:Issuer"] = "gatums-gourmet",
                ["Jwt:Audience"] = "gatums-gourmet-api",
                ["Jwt:AccessTokenMinutes"] = "15"
            })
            .Build();

    [Fact]
    public void Token_contem_uma_claim_perm_por_permissao_e_role_por_role()
    {
        var sut = new JwtTokenGenerator(BuildConfig());
        var userId = Guid.NewGuid();
        var email = "admin@gatums.com";
        var roles = new[] { "Admin", "Gerente" };
        var permissoes = new[] { "produtos:escrever", "pedidos:ler" };

        var jwt = sut.Generate(userId, email, roles, permissoes);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        var roleClaims = token.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();
        roleClaims.Should().BeEquivalentTo(new[] { "Admin", "Gerente" });

        var permClaims = token.Claims
            .Where(c => c.Type == "perm")
            .Select(c => c.Value)
            .ToList();
        permClaims.Should().BeEquivalentTo(new[] { "produtos:escrever", "pedidos:ler" });

        token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value
            .Should().Be(userId.ToString());
        token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value
            .Should().Be(email);
    }
}
