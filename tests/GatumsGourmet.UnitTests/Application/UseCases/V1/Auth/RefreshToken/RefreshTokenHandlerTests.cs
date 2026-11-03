using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Auth.RefreshToken;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Auth.RefreshToken;

public class RefreshTokenHandlerTests
{
    private readonly Mock<IRefreshTokenStore> _store = new();
    private readonly Mock<IRefreshTokenGenerator> _gen = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IPermissionResolver> _permissions = new();
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IConfiguration _config;
    private readonly DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7"
            })
            .Build();

        _clock.Setup(c => c.UtcNow).Returns(_now);

        _handler = new RefreshTokenHandler(
            _store.Object,
            _gen.Object,
            _jwt.Object,
            _permissions.Object,
            _usuarios.Object,
            _clock.Object,
            _config);
    }

    private static Usuario CreateUsuario(bool ativo = true)
        => new() { Id = Guid.NewGuid(), Nome = "Admin", Email = "admin@gatums.com", SenhaHash = "hash", Ativo = ativo };

    private global::GatumsGourmet.Domain.Entities.Auth.RefreshToken CreateToken(
        Guid usuarioId, bool revogado = false, DateTime? expiraEm = null, Guid? familiaId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TokenHash = "old-hash",
            FamiliaId = familiaId ?? Guid.NewGuid(),
            ExpiraEm = expiraEm ?? _now.AddDays(7),
            CriadoEm = _now.AddDays(-1),
            Revogado = revogado
        };

    [Fact]
    public async Task RefreshValido_RotacionaEEmiteNovos()
    {
        var usuario = CreateUsuario();
        var token = CreateToken(usuario.Id);
        _gen.Setup(g => g.Hash("plain-refresh")).Returns("old-hash");
        _store.Setup(s => s.GetByHashAsync("old-hash", default)).ReturnsAsync(token);
        _usuarios.Setup(u => u.GetByIdAsync(usuario.Id, default)).ReturnsAsync(usuario);
        _gen.Setup(g => g.Generate()).Returns(("novo-plain", "novo-hash"));
        _permissions.Setup(p => p.ResolveAsync(usuario.Id, default))
            .ReturnsAsync(((IReadOnlyList<string>)new[] { "Admin" }, (IReadOnlyList<string>)new[] { "x:y" }));
        _jwt.Setup(j => j.Generate(usuario.Id, usuario.Email,
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("jwt");

        var result = await _handler.Handle(new RefreshTokenRequest("plain-refresh"), "1.2.3.4");

        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("jwt");
        result.Data.RefreshToken.Should().Be("novo-plain");
        result.Data.ExpiresIn.Should().Be(900);
        result.Data.User.Roles.Should().Contain("Admin");

        token.Revogado.Should().BeTrue();
        token.SubstituidoPorTokenHash.Should().Be("novo-hash");
        token.MotivoRevogacao.Should().Be("Rotacionado");

        _store.Verify(s => s.AddAsync(
            It.Is<global::GatumsGourmet.Domain.Entities.Auth.RefreshToken>(
                t => t.TokenHash == "novo-hash"
                     && t.FamiliaId == token.FamiliaId
                     && !t.Revogado
                     && t.CriadoPorIp == "1.2.3.4"),
            default), Times.Once);
        _store.Verify(s => s.SaveChangesAsync(default), Times.Once);
        _permissions.Verify(p => p.ResolveAsync(usuario.Id, default), Times.Once);
    }

    [Fact]
    public async Task RefreshJaRevogado_RevogaFamiliaEFalha()
    {
        var usuario = CreateUsuario();
        var familiaId = Guid.NewGuid();
        var token = CreateToken(usuario.Id, revogado: true, familiaId: familiaId);
        _gen.Setup(g => g.Hash("plain-refresh")).Returns("old-hash");
        _store.Setup(s => s.GetByHashAsync("old-hash", default)).ReturnsAsync(token);

        var result = await _handler.Handle(new RefreshTokenRequest("plain-refresh"), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Refresh token inválido.");
        _store.Verify(s => s.RevokeFamilyAsync(familiaId, It.IsAny<string>(), _now, default), Times.Once);
        _store.Verify(s => s.AddAsync(It.IsAny<global::GatumsGourmet.Domain.Entities.Auth.RefreshToken>(), default), Times.Never);
    }

    [Fact]
    public async Task RefreshExpirado_Falha()
    {
        var usuario = CreateUsuario();
        var token = CreateToken(usuario.Id, expiraEm: _now.AddDays(-1));
        _gen.Setup(g => g.Hash("plain-refresh")).Returns("old-hash");
        _store.Setup(s => s.GetByHashAsync("old-hash", default)).ReturnsAsync(token);

        var result = await _handler.Handle(new RefreshTokenRequest("plain-refresh"), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Refresh token expirado.");
        _store.Verify(s => s.AddAsync(It.IsAny<global::GatumsGourmet.Domain.Entities.Auth.RefreshToken>(), default), Times.Never);
        _store.Verify(s => s.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task UsuarioInativo_Falha()
    {
        var usuario = CreateUsuario(ativo: false);
        var token = CreateToken(usuario.Id);
        _gen.Setup(g => g.Hash("plain-refresh")).Returns("old-hash");
        _store.Setup(s => s.GetByHashAsync("old-hash", default)).ReturnsAsync(token);
        _usuarios.Setup(u => u.GetByIdAsync(usuario.Id, default)).ReturnsAsync(usuario);

        var result = await _handler.Handle(new RefreshTokenRequest("plain-refresh"), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usuário inativo.");
        _store.Verify(s => s.AddAsync(It.IsAny<global::GatumsGourmet.Domain.Entities.Auth.RefreshToken>(), default), Times.Never);
    }

    [Fact]
    public async Task TokenInexistente_Falha()
    {
        _gen.Setup(g => g.Hash("plain-refresh")).Returns("old-hash");
        _store.Setup(s => s.GetByHashAsync("old-hash", default)).ReturnsAsync((global::GatumsGourmet.Domain.Entities.Auth.RefreshToken?)null);

        var result = await _handler.Handle(new RefreshTokenRequest("plain-refresh"), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Refresh token inválido.");
        _store.Verify(s => s.AddAsync(It.IsAny<global::GatumsGourmet.Domain.Entities.Auth.RefreshToken>(), default), Times.Never);
    }
}
