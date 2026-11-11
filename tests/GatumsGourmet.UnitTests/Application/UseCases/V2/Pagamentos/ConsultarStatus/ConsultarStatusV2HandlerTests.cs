using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.Application.UseCases.V2.Pagamentos;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.ConsultarStatus;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V2.Pagamentos.ConsultarStatus;

public class ConsultarStatusV2HandlerTests
{
    private const string Origem = "Consulta de status (Orders)";

    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPixOrdersProvider> _provider = new();
    private readonly Mock<IAplicarResultadoPagamentoPixService> _aplicar = new();
    private readonly Mock<ILogger<ConsultarStatusV2Handler>> _logger = new();
    private readonly ConsultarStatusV2Handler _handler;

    public ConsultarStatusV2HandlerTests()
    {
        _handler = new ConsultarStatusV2Handler(_transacoes.Object, _provider.Object, _aplicar.Object, _logger.Object);
    }

    private static TransacaoPagamento CreateTransacao(
        StatusTransacao status = StatusTransacao.Pendente,
        string operadora = "mercadopago-orders",
        string? operadoraRef = "ORD01TEST") => new()
    {
        Id = Guid.NewGuid(),
        PedidoId = Guid.NewGuid(),
        Metodo = MetodoPagamento.Pix,
        Valor = 50m,
        Status = status,
        Operadora = operadora,
        OperadoraRef = operadoraRef,
        CriadoEm = DateTime.UtcNow,
        AtualizadoEm = DateTime.UtcNow
    };

    private TransacaoPagamento Setup(TransacaoPagamento t)
    {
        _transacoes.Setup(r => r.GetByIdAsync(t.Id, default)).ReturnsAsync(t);
        return t;
    }

    [Fact]
    public async Task TransacaoInexistente_Fail()
    {
        var id = Guid.NewGuid();
        _transacoes.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((TransacaoPagamento?)null);

        var result = await _handler.Handle(id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não encontrada.");
    }

    [Fact]
    public async Task PendenteDaV2_ConsultaOrderEAplicaResultado()
    {
        var t = Setup(CreateTransacao());
        var status = new PixOrderStatus("processed", "accredited");
        _provider.Setup(p => p.ConsultarOrderAsync("ORD01TEST", default)).ReturnsAsync(status);
        _aplicar.Setup(a => a.AplicarAsync(t, status, Origem, default))
            .Callback<TransacaoPagamento, PixOrderStatus, string, CancellationToken>((tx, _, _, _) => tx.Status = StatusTransacao.Aprovado)
            .ReturnsAsync(true);

        var result = await _handler.Handle(t.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("aprovado");
        _aplicar.Verify(a => a.AplicarAsync(t, status, Origem, default), Times.Once);
    }

    [Fact]
    public async Task JaAprovada_NaoConsultaProvider()
    {
        var t = Setup(CreateTransacao(StatusTransacao.Aprovado));

        var result = await _handler.Handle(t.Id);

        result.IsSuccess.Should().BeTrue();
        _provider.Verify(p => p.ConsultarOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _aplicar.Verify(a => a.AplicarAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<PixOrderStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("mercadopago", "123")]
    [InlineData("mercadopago-orders", null)]
    [InlineData("mercadopago-orders", " ")]
    public async Task TransacaoDaV1OuSemRef_NaoConsultaProvider_RetornaComoEsta(string operadora, string? operadoraRef)
    {
        var t = Setup(CreateTransacao(operadora: operadora, operadoraRef: operadoraRef));

        var result = await _handler.Handle(t.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("pendente");
        _provider.Verify(p => p.ConsultarOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _aplicar.Verify(a => a.AplicarAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<PixOrderStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProviderRetornaNull_MantemPendente()
    {
        var t = Setup(CreateTransacao());
        _provider.Setup(p => p.ConsultarOrderAsync("ORD01TEST", default)).ReturnsAsync((PixOrderStatus?)null);

        var result = await _handler.Handle(t.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("pendente");
        _aplicar.Verify(a => a.AplicarAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<PixOrderStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProviderLancaPixOrdersProviderException_MantemPendente_NaoQuebra_ELogaOrderId()
    {
        var t = Setup(CreateTransacao());
        _provider.Setup(p => p.ConsultarOrderAsync("ORD01TEST", default))
            .ThrowsAsync(new PixOrdersProviderException("falha", 500));

        var result = await _handler.Handle(t.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("pendente");
        VerificaWarningComOrderId();
    }

    [Fact]
    public async Task ProviderLancaJsonException_MantemPendente_NaoQuebra_ELogaOrderId()
    {
        var t = Setup(CreateTransacao());
        _provider.Setup(p => p.ConsultarOrderAsync("ORD01TEST", default))
            .ThrowsAsync(new JsonException("corpo inválido"));

        var result = await _handler.Handle(t.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("pendente");
        VerificaWarningComOrderId();
    }

    [Fact]
    public async Task ProviderLancaOutraExcecao_Propaga()
    {
        var t = Setup(CreateTransacao());
        _provider.Setup(p => p.ConsultarOrderAsync("ORD01TEST", default))
            .ThrowsAsync(new InvalidOperationException("bug"));

        var act = () => _handler.Handle(t.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private void VerificaWarningComOrderId() =>
        _logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("ORD01TEST")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
}
