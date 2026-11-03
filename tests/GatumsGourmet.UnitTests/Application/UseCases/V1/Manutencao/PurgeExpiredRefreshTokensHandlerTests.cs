using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Manutencao.PurgeExpiredRefreshTokens;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Manutencao;

public class PurgeExpiredRefreshTokensHandlerTests
{
    private readonly Mock<IRefreshTokenStore> _store = new();
    private readonly Mock<IClock> _clock = new();
    private readonly DateTime _now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    public PurgeExpiredRefreshTokensHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(_now);
    }

    private static IConfiguration BuildConfig(params (string key, string? value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(p => p.key, p => p.value))
            .Build();

    private PurgeExpiredRefreshTokensHandler CreateHandler(IConfiguration config) =>
        new(_store.Object, _clock.Object, config);

    [Fact]
    public async Task Handle_RemoveExpirados_RetornaContagem()
    {
        var config = BuildConfig(("Auth:RefreshTokenRetentionDays", "30"));
        _store.Setup(s => s.DeleteExpiredAsync(_now.AddDays(-30), default)).ReturnsAsync(7);
        var handler = CreateHandler(config);

        var result = await handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(7);
        _store.Verify(s => s.DeleteExpiredAsync(_now.AddDays(-30), default), Times.Once);
    }

    [Fact]
    public async Task Handle_RetentionConfiguravel()
    {
        var config = BuildConfig(("Auth:RefreshTokenRetentionDays", "10"));
        _store.Setup(s => s.DeleteExpiredAsync(It.IsAny<DateTime>(), default)).ReturnsAsync(3);
        var handler = CreateHandler(config);

        var result = await handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(3);
        _store.Verify(s => s.DeleteExpiredAsync(_now.AddDays(-10), default), Times.Once);
    }

    [Fact]
    public async Task Handle_SemConfig_UsaDefault30Dias()
    {
        var config = BuildConfig();
        _store.Setup(s => s.DeleteExpiredAsync(It.IsAny<DateTime>(), default)).ReturnsAsync(0);
        var handler = CreateHandler(config);

        var result = await handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(0);
        _store.Verify(s => s.DeleteExpiredAsync(_now.AddDays(-30), default), Times.Once);
    }
}
