using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Imagens;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Imagens;

public class ImageTypeDetectorTests
{
    public static IEnumerable<object[]> CasosReconhecidos()
    {
        // JPEG: FF D8 FF
        yield return new object[]
        {
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "jpg", "image/jpeg"
        };

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        yield return new object[]
        {
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", "image/png"
        };

        // WebP: RIFF ???? WEBP
        yield return new object[]
        {
            new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, "webp", "image/webp"
        };
    }

    [Theory]
    [MemberData(nameof(CasosReconhecidos))]
    public void Detectar_AssinaturaConhecida_RetornaTipo(byte[] prefixo, string extensaoEsperada, string contentTypeEsperado)
    {
        var (ok, extensao, contentType) = ImageTypeDetector.Detectar(prefixo);

        ok.Should().BeTrue();
        extensao.Should().Be(extensaoEsperada);
        contentType.Should().Be(contentTypeEsperado);
    }

    [Fact]
    public void Detectar_Texto_RetornaNaoReconhecido()
    {
        // "Hello"
        var prefixo = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        var (ok, extensao, contentType) = ImageTypeDetector.Detectar(prefixo);

        ok.Should().BeFalse();
        extensao.Should().BeEmpty();
        contentType.Should().BeEmpty();
    }

    [Fact]
    public void Detectar_BufferVazio_RetornaNaoReconhecido()
    {
        var (ok, extensao, contentType) = ImageTypeDetector.Detectar(Array.Empty<byte>());

        ok.Should().BeFalse();
        extensao.Should().BeEmpty();
        contentType.Should().BeEmpty();
    }
}
