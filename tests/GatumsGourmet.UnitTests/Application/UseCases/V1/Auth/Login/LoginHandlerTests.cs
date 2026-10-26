using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Auth.Login;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Auth.Login;

public class LoginHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshGen = new();
    private readonly Mock<IRefreshTokenStore> _refreshStore = new();
    private readonly Mock<IPermissionResolver> _permissions = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IConfiguration _config;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7"
            })
            .Build();

        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _handler = new LoginHandler(
            _usuarios.Object,
            _hasher.Object,
            _jwt.Object,
            _refreshGen.Object,
            _refreshStore.Object,
            _permissions.Object,
            _clock.Object,
            _config);
    }

    private static Usuario CreateUsuario(string email = "admin@gatums.com", bool ativo = true)
        => new() { Id = Guid.NewGuid(), Nome = "Admin", Email = email, SenhaHash = "hash", Ativo = ativo };

    [Fact]
    public async Task Handle_CredenciaisValidas_RetornaAccessERefresh()
    {
        var usuario = CreateUsuario();
        _usuarios.Setup(r => r.GetByEmailAsync(usuario.Email, default)).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify("Admin@123", "hash")).Returns(true);
        _permissions.Setup(p => p.ResolveAsync(usuario.Id, default))
            .ReturnsAsync(((IReadOnlyList<string>)new[] { "Admin" }, (IReadOnlyList<string>)new[] { "x:y" }));
        _jwt.Setup(j => j.Generate(usuario.Id, usuario.Email,
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("jwt");
        _refreshGen.Setup(g => g.Generate()).Returns(("plain", "hash"));

        var result = await _handler.Handle(new LoginRequest(usuario.Email, "Admin@123"));

        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("jwt");
        result.Data.RefreshToken.Should().Be("plain");
        result.Data.ExpiresIn.Should().Be(900);
        result.Data.User.Roles.Should().Contain("Admin");
        _refreshStore.Verify(s => s.AddAsync(It.IsAny<RefreshToken>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_UsuarioInexistente_RetornaErro()
    {
        _usuarios.Setup(r => r.GetByEmailAsync("inexistente@x.com", default)).ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(new LoginRequest("inexistente@x.com", "qualquer"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Credenciais inválidas.");
        _refreshStore.Verify(s => s.AddAsync(It.IsAny<RefreshToken>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_SenhaIncorreta_RetornaErro()
    {
        var usuario = CreateUsuario();
        _usuarios.Setup(r => r.GetByEmailAsync(usuario.Email, default)).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify("errada", "hash")).Returns(false);

        var result = await _handler.Handle(new LoginRequest(usuario.Email, "errada"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Credenciais inválidas.");
        _refreshStore.Verify(s => s.AddAsync(It.IsAny<RefreshToken>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_UsuarioInativo_RetornaErro()
    {
        var usuario = CreateUsuario(ativo: false);
        _usuarios.Setup(r => r.GetByEmailAsync(usuario.Email, default)).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify("Admin@123", "hash")).Returns(true);

        var result = await _handler.Handle(new LoginRequest(usuario.Email, "Admin@123"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usuário inativo.");
        _refreshStore.Verify(s => s.AddAsync(It.IsAny<RefreshToken>(), default), Times.Never);
    }
}
