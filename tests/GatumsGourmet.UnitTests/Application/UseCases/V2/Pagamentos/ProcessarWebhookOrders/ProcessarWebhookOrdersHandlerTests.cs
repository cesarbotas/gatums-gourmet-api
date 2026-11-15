using FluentAssertions;
using GatumsGourmet.Application.UseCases.V2.Pagamentos;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.ProcessarWebhookOrders;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V2.Pagamentos.ProcessarWebhookOrders;

public class ProcessarWebhookOrdersHandlerTests
{
    private const string Origem = "Webhook Mercado Pago (Orders)";
    private const string OrderId = "ORD01TEST";

    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPixOrdersProvider> _provider = new();
    private readonly Mock<IAplicarResultadoPagamentoPixService> _aplicar = new();
    private readonly Mock<ILogger<ProcessarWebhookOrdersHandler>> _logger = new();
    private readonly ProcessarWebhookOrdersHandler _handler;

    public ProcessarWebhookOrdersHandlerTests()
    {
        _handler = new ProcessarWebhookOrdersHandler(_transacoes.Object, _provider.Object, _aplicar.Object, _logger.Object);
    }

    private static TransacaoPagamento CreateTransacao(StatusTransacao status = StatusTransacao.Pendente) => new()
    {
        Id = Guid.NewGuid(),
        PedidoId = Guid.NewGuid(),
        Metodo = MetodoPagamento.Pix,
        Valor = 50m,
        Status = status,
        Operadora = "mercadopago-orders",
        OperadoraRef = OrderId,
        CriadoEm = DateTime.UtcNow,
        AtualizadoEm = DateTime.UtcNow
    };

    private void VerificaNaoConsultouNemAplicou()
    {
        _provider.Verify(p => p.ConsultarOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _aplicar.Verify(a => a.AplicarAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<PixOrderStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task SemDataId_Ok_Ignora(string? dataId)
    {
        var result = await _handler.Handle(dataId, "order");

        result.IsSuccess.Should().BeTrue();
        VerificaNaoConsultouNemAplicou();
        _transacoes.Verify(r => r.GetByOperadoraRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("payment")]
    [InlineData(null)]
    [InlineData("")]
    public async Task TypeDiferenteDeOrder_Ok_Ignora_NaoConsultaProvider(string? type)
    {
        var result = await _handler.Handle(OrderId, type);

        result.IsSuccess.Should().BeTrue();
        VerificaNaoConsultouNemAplicou();
    }

    [Theory]
    [InlineData("order")]
    [InlineData("ORDER")]
    public async Task OrderPaga_AplicaResultado(string type)
    {
        var t = CreateTransacao();
        var status = new PixOrderStatus("processed", "accredited");
        _transacoes.Setup(r => r.GetByOperadoraRefAsync(OrderId, default)).ReturnsAsync(t);
        _provider.Setup(p => p.ConsultarOrderAsync(OrderId, default)).ReturnsAsync(status);
        _aplicar.Setup(a => a.AplicarAsync(t, status, Origem, default)).ReturnsAsync(true);

        var result = await _handler.Handle(OrderId, type);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        _aplicar.Verify(a => a.AplicarAsync(t, status, Origem, default), Times.Once);
    }

    [Fact]
    public async Task OrderDesconhecida_Ok_NaoQuebra()
    {
        _transacoes.Setup(r => r.GetByOperadoraRefAsync(OrderId, default)).ReturnsAsync((TransacaoPagamento?)null);

        var result = await _handler.Handle(OrderId, "order");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        VerificaNaoConsultouNemAplicou();
    }

    [Fact]
    public async Task Reenvio_TransacaoJaAprovada_Idempotente()
    {
        var t = CreateTransacao(StatusTransacao.Aprovado);
        var status = new PixOrderStatus("processed", "accredited");
        _transacoes.Setup(r => r.GetByOperadoraRefAsync(OrderId, default)).ReturnsAsync(t);
        _provider.Setup(p => p.ConsultarOrderAsync(OrderId, default)).ReturnsAsync(status);
        _aplicar.Setup(a => a.AplicarAsync(t, status, Origem, default)).ReturnsAsync(false);

        var result = await _handler.Handle(OrderId, "order");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ProviderRetornaNull_Ok_SemAlterar()
    {
        var t = CreateTransacao();
        _transacoes.Setup(r => r.GetByOperadoraRefAsync(OrderId, default)).ReturnsAsync(t);
        _provider.Setup(p => p.ConsultarOrderAsync(OrderId, default)).ReturnsAsync((PixOrderStatus?)null);

        var result = await _handler.Handle(OrderId, "order");

        result.IsSuccess.Should().BeTrue();
        t.Status.Should().Be(StatusTransacao.Pendente);
        _aplicar.Verify(a => a.AplicarAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<PixOrderStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProviderLanca_Propaga()
    {
        var t = CreateTransacao();
        _transacoes.Setup(r => r.GetByOperadoraRefAsync(OrderId, default)).ReturnsAsync(t);
        _provider.Setup(p => p.ConsultarOrderAsync(OrderId, default))
            .ThrowsAsync(new PixOrdersProviderException("falha", 500));

        var act = () => _handler.Handle(OrderId, "order");

        await act.Should().ThrowAsync<PixOrdersProviderException>();
        _aplicar.Verify(a => a.AplicarAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<PixOrderStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
