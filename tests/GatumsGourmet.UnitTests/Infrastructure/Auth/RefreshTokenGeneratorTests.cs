using FluentAssertions;
using GatumsGourmet.Infrastructure.Services.Auth;

namespace GatumsGourmet.UnitTests.Infrastructure.Auth;

public class RefreshTokenGeneratorTests
{
    [Fact]
    public void Hash_e_estavel_e_plain_difere_do_hash()
    {
        var sut = new RefreshTokenGenerator();

        var (plain, hash) = sut.Generate();

        plain.Should().NotBe(hash);

        var hash1 = sut.Hash(plain);
        var hash2 = sut.Hash(plain);
        hash1.Should().Be(hash2);

        sut.Hash(plain).Should().Be(hash);
    }
}
