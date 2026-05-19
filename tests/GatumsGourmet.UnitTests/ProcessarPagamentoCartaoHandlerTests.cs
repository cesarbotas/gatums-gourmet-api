using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Pagamentos.ProcessarPagamentoCartao;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GatumsGourmet.UnitTests;

public class ProcessarPagamentoCartaoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ProcessarPagamentoCartaoHandler _handler;

    public ProcessarPagamentoCartaoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        _handler = new ProcessarPagamentoCartaoHandler(_transacoes.Object, _gateway.Object, _clock.Object, NullLogger<ProcessarPagamentoCartaoHandler>.Instance);
    }

    [Fact]
    public async Task Handle_TransacaoNaoEncontrada_RetornaErro()
    {
        _transacoes.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((TransacaoPagamento?)null);

        var result = await _handler.Handle(Guid.NewGuid(), new ProcessarPagamentoCartaoRequest("4111111111111111", "João", "12/26", "123"), default);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GatewayAprovado_AtualizaStatusAprovado()
    {
        var transacao = new TransacaoPagamento { Id = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Metodo = MetodoPagamento.CartaoCredito, Valor = 100m, Status = StatusTransacao.Pendente, Operadora = "mock", CriadoEm = DateTime.UtcNow, AtualizadoEm = DateTime.UtcNow };
        _transacoes.Setup(t => t.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _gateway.Setup(g => g.ProcessarAsync(transacao.Id, 100m, MetodoPagamento.CartaoCredito, It.IsAny<CartaoInput>(), default))
            .ReturnsAsync(new CartaoResult(true, "ref-123", "1111", "VISA", null));
        _transacoes.Setup(t => t.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(transacao.Id, new ProcessarPagamentoCartaoRequest("4111111111111111", "João", "12/26", "123"), default);

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Aprovado);
        transacao.CartaoUltimos4.Should().Be("1111");
    }

    [Fact]
    public async Task Handle_GatewayRecusado_AtualizaStatusRecusado()
    {
        var transacao = new TransacaoPagamento { Id = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Metodo = MetodoPagamento.CartaoCredito, Valor = 100m, Status = StatusTransacao.Pendente, Operadora = "mock", CriadoEm = DateTime.UtcNow, AtualizadoEm = DateTime.UtcNow };
        _transacoes.Setup(t => t.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _gateway.Setup(g => g.ProcessarAsync(transacao.Id, 100m, MetodoPagamento.CartaoCredito, It.IsAny<CartaoInput>(), default))
            .ReturnsAsync(new CartaoResult(false, null, null, null, "Saldo insuficiente"));
        _transacoes.Setup(t => t.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(transacao.Id, new ProcessarPagamentoCartaoRequest("4111111111111111", "João", "12/26", "123"), default);

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Recusado);
        transacao.MensagemErro.Should().Be("Saldo insuficiente");
    }
}
