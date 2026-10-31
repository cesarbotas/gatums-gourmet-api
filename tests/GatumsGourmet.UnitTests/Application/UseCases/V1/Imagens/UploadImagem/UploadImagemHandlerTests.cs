using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Imagens.UploadImagem;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Imagens.UploadImagem;

public class UploadImagemHandlerTests
{
    private readonly Mock<IImageStorage> _storage = new();
    private readonly IConfiguration _config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:MaxFileSizeMB"] = "5"
        })
        .Build();

    private UploadImagemHandler CriarHandler() => new(_storage.Object, _config);

    private static MemoryStream StreamComBytes(params byte[] bytes) => new(bytes);

    [Fact]
    public async Task Handle_ImagemJpegValida_RetornaUrl()
    {
        // JPEG: FF D8 FF E0 + conteúdo extra
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var stream = StreamComBytes(bytes);

        _storage
            .Setup(s => s.UploadAsync(
                It.IsAny<Stream>(),
                "image/jpeg",
                "jpg",
                "produtos",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageUploadResult("https://x/produtos/a.jpg", "produtos/a.jpg"));

        var handler = CriarHandler();

        var result = await handler.Handle(stream, "image/jpeg", "foto.jpg", bytes.Length);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Url.Should().Be("https://x/produtos/a.jpg");
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(),
            "image/jpeg",
            "jpg",
            "produtos",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Vazio_Falha()
    {
        var stream = StreamComBytes();
        var handler = CriarHandler();

        var result = await handler.Handle(stream, "image/jpeg", "foto.jpg", 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Arquivo vazio.");
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AcimaDoLimite_Falha()
    {
        // O stream pode ser pequeno; o parâmetro tamanho é o que importa.
        var stream = StreamComBytes(0xFF, 0xD8, 0xFF);
        var handler = CriarHandler();

        var result = await handler.Handle(stream, "image/jpeg", "foto.jpg", 6L * 1024 * 1024);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("tamanho máximo");
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConteudoNaoImagem_Falha()
    {
        // "Hello..." — content-type mentido não engana a detecção por magic bytes.
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2E, 0x2E, 0x2E };
        var stream = StreamComBytes(bytes);
        var handler = CriarHandler();

        var result = await handler.Handle(stream, "image/jpeg", "foto.jpg", bytes.Length);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Tipo de imagem não suportado. Use JPEG, PNG ou WebP.");
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
