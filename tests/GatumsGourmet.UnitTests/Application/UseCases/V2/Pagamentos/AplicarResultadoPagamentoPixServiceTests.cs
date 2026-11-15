using FluentAssertions;
using GatumsGourmet.Application.UseCases.V2.Pagamentos;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V2.Pagamentos;

public class AplicarResultadoPagamentoPixServiceTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IClock> _clock = new();
    private readonly AplicarResultadoPagamentoPixService _service;

    public AplicarResultadoPagamentoPixServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        _service = new AplicarResultadoPagamentoPixService(
            _transacoes.Object,
            _pedidos.Object,
            _clock.Object,
            Mock.Of<ILogger<AplicarResultadoPagamentoPixService>>());
    }

    [Theory]
    [InlineData("processed", "accredited", StatusTransacao.Aprovado)]
    [InlineData("canceled", "expired", StatusTransacao.Recusado)]
    [InlineData("canceled", "canceled", StatusTransacao.Recusado)]
    [InlineData("failed", "qualquer", StatusTransacao.Recusado)]
    [InlineData("failed", null, StatusTransacao.Recusado)]
    public void Mapper_StatusFinal_MapeiaParaStatusTransacao(string status, string? detail, StatusTransacao esperado)
    {
        PixOrderStatusMapper.ToStatusTransacao(new PixOrderStatus(status, detail)).Should().Be(esperado);
    }

    [Theory]
    [InlineData("action_required", "waiting_transfer")]
    [InlineData("processing", "in_process")]
    public void Mapper_StatusPendente_RetornaNull(string status, string detail)
    {
        PixOrderStatusMapper.ToStatusTransacao(new PixOrderStatus(status, detail)).Should().BeNull();
    }

    private static TransacaoPagamento NovaTransacao(Guid pedidoId, StatusTransacao status = StatusTransacao.Pendente) => new()
    {
        Id = Guid.NewGuid(),
        PedidoId = pedidoId,
        Status = status,
        Operadora = "mercadopago-orders",
        OperadoraRef = "ORD123"
    };

    [Fact]
    public async Task Aprovado_AtualizaTransacaoEPedido()
    {
        var pedido = new Pedido { Id = Guid.NewGuid(), StatusAtual = PedidoEtapa.Criado };
        var transacao = NovaTransacao(pedido.Id);
        _pedidos.Setup(p => p.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);

        var mudou = await _service.AplicarAsync(transacao, new PixOrderStatus("processed", "accredited"), "webhook");

        mudou.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Aprovado);
        pedido.StatusAtual.Should().Be(PedidoEtapa.Pagamento);
        pedido.Pagamento!.Resumo.Should().Be("Pix (Mercado Pago)");
        pedido.Pagamento.TransacaoId.Should().Be(transacao.Id);
        pedido.Timeline.Should().ContainSingle(e => e.Etapa == PedidoEtapa.Pagamento && e.Observacao!.Contains("webhook"));
        _transacoes.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _pedidos.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Recusado_MarcaTransacaoRecusada_SemTocarNoPedido()
    {
        var transacao = NovaTransacao(Guid.NewGuid());

        var mudou = await _service.AplicarAsync(transacao, new PixOrderStatus("canceled", "expired"), "webhook");

        mudou.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Recusado);
        _pedidos.Verify(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _pedidos.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transacoes.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task TransacaoJaAprovada_NaoAlteraNada_RetornaFalse()
    {
        var pedido = new Pedido { Id = Guid.NewGuid(), StatusAtual = PedidoEtapa.Pagamento };
        var transacao = NovaTransacao(pedido.Id, StatusTransacao.Aprovado);

        var mudou = await _service.AplicarAsync(transacao, new PixOrderStatus("processed", "accredited"), "webhook");

        mudou.Should().BeFalse();
        transacao.Status.Should().Be(StatusTransacao.Aprovado);
        pedido.Timeline.Should().BeEmpty();
        _transacoes.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _pedidos.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StatusPendente_NaoAltera()
    {
        var transacao = NovaTransacao(Guid.NewGuid());

        var mudou = await _service.AplicarAsync(transacao, new PixOrderStatus("action_required", "waiting_transfer"), "webhook");

        mudou.Should().BeFalse();
        transacao.Status.Should().Be(StatusTransacao.Pendente);
        _transacoes.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}