using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Enderecos.BuscarEnderecoPorCep;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Enderecos.BuscarEnderecoPorCep;

public class BuscarEnderecoPorCepHandlerTests
{
    private readonly Mock<ICepProvider> _provider = new();
    private readonly BuscarEnderecoPorCepHandler _handler;

    public BuscarEnderecoPorCepHandlerTests()
    {
        _handler = new BuscarEnderecoPorCepHandler(
            _provider.Object,
            Mock.Of<ILogger<BuscarEnderecoPorCepHandler>>());
    }

    [Fact]
    public async Task Handle_CepValido_RetornaEndereco()
    {
        _provider.Setup(p => p.BuscarAsync("01001000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnderecoCep("01001000", "Praça da Sé", "lado ímpar", "Sé", "São Paulo", "SP"));

        var result = await _handler.Handle("01001000");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Cep.Should().Be("01001000");
        result.Data.Logradouro.Should().Be("Praça da Sé");
        result.Data.Cidade.Should().Be("São Paulo");
        result.Data.Estado.Should().Be("SP");
    }

    [Fact]
    public async Task Handle_CepComMascara_NormalizaAntesDeBuscar()
    {
        _provider.Setup(p => p.BuscarAsync("01001000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnderecoCep("01001000", "Praça da Sé", null, "Sé", "São Paulo", "SP"));

        var result = await _handler.Handle("01001-000");

        result.IsSuccess.Should().BeTrue();
        _provider.Verify(p => p.BuscarAsync("01001000", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("abcdefgh")]
    [InlineData("")]
    public async Task Handle_CepFormatoInvalido_RetornaFalhaSemChamarProvider(string cep)
    {
        var result = await _handler.Handle(cep);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("CEP inválido.");
        _provider.Verify(p => p.BuscarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CepNaoEncontrado_RetornaFalha()
    {
        _provider.Setup(p => p.BuscarAsync("99999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnderecoCep?)null);

        var result = await _handler.Handle("99999999");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("CEP não encontrado.");
    }

    [Fact]
    public async Task Handle_ProviderLancaExcecao_RetornaServicoIndisponivel()
    {
        _provider.Setup(p => p.BuscarAsync("01001000", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var result = await _handler.Handle("01001000");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Serviço de CEP indisponível.");
    }
}
