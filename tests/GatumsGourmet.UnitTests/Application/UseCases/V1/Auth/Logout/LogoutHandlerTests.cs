using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Auth.Logout;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Moq;
using Xunit;
using RefreshTokenEntity = GatumsGourmet.Domain.Entities.Auth.RefreshToken;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Auth.Logout;

public class LogoutHandlerTests
{
    private readonly Mock<IRefreshTokenStore> _store = new();
    private readonly Mock<IRefreshTokenGenerator> _gen = new();
    private readonly Mock<IClock> _clock = new();
    private readonly DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(_now);
        _handler = new LogoutHandler(_store.Object, _gen.Object, _clock.Object);
    }

    [Fact]
    public async Task TokenDoUsuario_RevogaFamilia()
    {
        var usuarioId = Guid.NewGuid();
        var familiaId = Guid.NewGuid();
        var token = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TokenHash = "h",
            FamiliaId = familiaId,
            ExpiraEm = _now.AddDays(7),
            CriadoEm = _now,
            Revogado = false
        };
        _gen.Setup(g => g.Hash("plain")).Returns("h");
        _store.Setup(s => s.GetByHashAsync("h", default)).ReturnsAsync(token);

        var result = await _handler.Handle(usuarioId, new LogoutRequest("plain"));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        _store.Verify(s => s.RevokeFamilyAsync(familiaId, "Logout", _now, default), Times.Once);
    }

    [Fact]
    public async Task TokenDeOutroUsuario_NaoRevoga_MasRetornaOk()
    {
        var usuarioId = Guid.NewGuid();
        var token = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UsuarioId = Guid.NewGuid(),
            TokenHash = "h",
            FamiliaId = Guid.NewGuid(),
            ExpiraEm = _now.AddDays(7),
            CriadoEm = _now,
            Revogado = false
        };
        _gen.Setup(g => g.Hash("plain")).Returns("h");
        _store.Setup(s => s.GetByHashAsync("h", default)).ReturnsAsync(token);

        var result = await _handler.Handle(usuarioId, new LogoutRequest("plain"));

        result.IsSuccess.Should().BeTrue();
        _store.Verify(s => s.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task TokenInexistente_RetornaOk_SemRevogar()
    {
        var usuarioId = Guid.NewGuid();
        _gen.Setup(g => g.Hash("plain")).Returns("h");
        _store.Setup(s => s.GetByHashAsync("h", default)).ReturnsAsync((RefreshTokenEntity?)null);

        var result = await _handler.Handle(usuarioId, new LogoutRequest("plain"));

        result.IsSuccess.Should().BeTrue();
        _store.Verify(s => s.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), default), Times.Never);
    }
}
