using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Imagens.PurgeOrphanImagens;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Imagens.PurgeOrphanImagens;

/// <summary>
/// Regras de limpeza de imagens órfãs:
/// - "órfã" = objeto no prefixo produtos/ cuja URL NÃO está em nenhum FotoUrl de produto;
/// - grace: só apaga objetos modificados ANTES do início do dia de hoje (00:00 UTC) —
///   tudo enviado hoje fica protegido (ex.: upload feito mas produto ainda não salvo).
/// </summary>
public class PurgeOrphanImagensHandlerTests
{
    private readonly Mock<IImageStorage> _storage = new();
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<IClock> _clock = new();

    // "Agora" = 2026-11-02 14:30 UTC → corte do grace = 2026-11-02 00:00 UTC.
    private static readonly DateTime Agora = new(2026, 11, 2, 14, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime Ontem = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime HojeCedo = new(2026, 11, 2, 2, 0, 0, DateTimeKind.Utc);

    public PurgeOrphanImagensHandlerTests() => _clock.SetupGet(c => c.UtcNow).Returns(Agora);

    private PurgeOrphanImagensHandler CriarHandler() =>
        new(_storage.Object, _produtos.Object, _clock.Object);

    private static StoredObject Obj(string key, DateTime modificadoUtc) =>
        new(key, $"https://cdn/{key}", modificadoUtc);

    [Fact]
    public async Task Handle_ObjetoOrfaoEAntigo_Deleta()
    {
        _storage.Setup(s => s.ListObjectsAsync("produtos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Obj("produtos/orfa.jpg", Ontem) });
        _produtos.Setup(p => p.ListFotoUrlsEmUsoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var result = await CriarHandler().Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(1);
        _storage.Verify(s => s.DeleteAsync("produtos/orfa.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ObjetoEmUso_NaoDeleta()
    {
        _storage.Setup(s => s.ListObjectsAsync("produtos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Obj("produtos/usada.jpg", Ontem) });
        _produtos.Setup(p => p.ListFotoUrlsEmUsoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "https://cdn/produtos/usada.jpg" });

        var result = await CriarHandler().Handle();

        result.Data.Should().Be(0);
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrfaMasDeHoje_NaoDeleta_PeloGrace()
    {
        _storage.Setup(s => s.ListObjectsAsync("produtos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Obj("produtos/recente.jpg", HojeCedo) });
        _produtos.Setup(p => p.ListFotoUrlsEmUsoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var result = await CriarHandler().Handle();

        result.Data.Should().Be(0);
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Misto_DeletaSoAsOrfasAntigas()
    {
        _storage.Setup(s => s.ListObjectsAsync("produtos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                Obj("produtos/orfa-antiga.jpg", Ontem),   // deleta
                Obj("produtos/usada.jpg", Ontem),         // em uso → mantém
                Obj("produtos/orfa-hoje.jpg", HojeCedo),  // grace → mantém
            });
        _produtos.Setup(p => p.ListFotoUrlsEmUsoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "https://cdn/produtos/usada.jpg" });

        var result = await CriarHandler().Handle();

        result.Data.Should().Be(1);
        _storage.Verify(s => s.DeleteAsync("produtos/orfa-antiga.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.DeleteAsync("produtos/usada.jpg", It.IsAny<CancellationToken>()), Times.Never);
        _storage.Verify(s => s.DeleteAsync("produtos/orfa-hoje.jpg", It.IsAny<CancellationToken>()), Times.Never);
    }
}
