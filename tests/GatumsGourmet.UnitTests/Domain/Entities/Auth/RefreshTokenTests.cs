using FluentAssertions;
using GatumsGourmet.Domain.Entities.Auth;
using Xunit;

namespace GatumsGourmet.UnitTests.Domain.Entities.Auth;

public class RefreshTokenTests
{
    [Fact]
    public void Novo_refresh_token_nao_nasce_revogado()
    {
        var token = new RefreshToken();

        token.Revogado.Should().BeFalse();
        token.RevogadoEm.Should().BeNull();
    }
}
