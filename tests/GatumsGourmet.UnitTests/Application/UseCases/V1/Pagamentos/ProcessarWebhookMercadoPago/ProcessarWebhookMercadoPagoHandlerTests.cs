using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarWebhookMercadoPago;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pagamentos.ProcessarWebhookMercadoPago;

public class ProcessarWebhookMercadoPagoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IPixProvider> _pix = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ProcessarWebhookMercadoPagoHandler _handler;

    public ProcessarWebhookMercadoPagoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new ProcessarWebhookMercadoPagoHandler(
            _transacoes.Object,
            _pedidos.Object,
            _pix.Object,
            _clock.Object,
            Mock.Of<ILogger<ProcessarWebhookMercadoPagoHandler>>());
    }

    [Fact]
    public async Task Handle_SemPaymentId_RetornaSucessoSemProcessar()
    {
        var result = await _handler.Handle(null, null);

        result.IsSuccess.Should().BeTrue();
        _transacoes.Verify(r => r.GetByOperadoraRefAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_PagamentoAprovadoNoMercadoPago_AtualizaTransacaoEPedido()
    {
        var paymentId = "123456789";
        var pedidoId = Guid.NewGuid();

        _pix.Setup(p => p.ConsultarStatusAsync(paymentId, default)).ReturnsAsync("approved");

        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = pedidoId,
            Metodo = MetodoPagamento.Pix,
            Status = StatusTransacao.Pendente,
            Operadora = "mercadopago",
            OperadoraRef = paymentId
        };
        _transacoes.Setup(r => r.GetByOperadoraRefAsync(paymentId, default)).ReturnsAsync(transacao);

        var pedido = new Pedido
        {
            Id = pedidoId,
            ClienteNome = "Cliente",
            StatusAtual = PedidoEtapa.Criado
        };
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(pedido);

        var body = new MercadoPagoWebhookBody
        {
            Action = "payment.updated",
            Data = new MercadoPagoWebhookData { Id = paymentId }
        };

        var result = await _handler.Handle(null, body);

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Aprovado);
        pedido.StatusAtual.Should().Be(PedidoEtapa.Pagamento);
        pedido.Pagamento.Should().NotBeNull();
        pedido.Pagamento!.Resumo.Should().Be("Pix (Mercado Pago)");

        _transacoes.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _pedidos.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_PagamentoRejeitadoNoMercadoPago_MarcaTransacaoRecusada()
    {
        var paymentId = "987654321";
        _pix.Setup(p => p.ConsultarStatusAsync(paymentId, default)).ReturnsAsync("rejected");

        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = Guid.NewGuid(),
            Metodo = MetodoPagamento.Pix,
            Status = StatusTransacao.Pendente,
            Operadora = "mercadopago",
            OperadoraRef = paymentId
        };
        _transacoes.Setup(r => r.GetByOperadoraRefAsync(paymentId, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(paymentId, null);

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Recusado);
        _transacoes.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}

