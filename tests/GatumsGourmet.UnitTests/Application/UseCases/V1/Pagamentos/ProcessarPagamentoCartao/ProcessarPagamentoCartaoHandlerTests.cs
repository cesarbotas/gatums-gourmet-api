using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarPagamentoCartao;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pagamentos.ProcessarPagamentoCartao;

public class ProcessarPagamentoCartaoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ProcessarPagamentoCartaoHandler _handler;

    public ProcessarPagamentoCartaoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new ProcessarPagamentoCartaoHandler(
            _transacoes.Object, _gateway.Object, _clock.Object,
            Mock.Of<ILogger<ProcessarPagamentoCartaoHandler>>());
    }

    private static TransacaoPagamento CreateTransacao(StatusTransacao status) => new()
    {
        Id = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Metodo = MetodoPagamento.CartaoCredito,
        Valor = 50m, Status = status, Operadora = "mock_gateway"
    };

    private static ProcessarPagamentoCartaoRequest Request() => new("4111111111111111", "Fulano", "12/30", "123");

    [Fact]
    public async Task Handle_TransacaoInexistente_RetornaErro()
    {
        _transacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((TransacaoPagamento?)null);

        var result = await _handler.Handle(Guid.NewGuid(), Request());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não encontrada.");
    }

    [Fact]
    public async Task Handle_TransacaoJaProcessada_RetornaErro()
    {
        var transacao = CreateTransacao(StatusTransacao.Aprovado);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(transacao.Id, Request());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação já processada.");
    }

    [Fact]
    public async Task Handle_GatewayAprova_MarcaAprovado()
    {
        var transacao = CreateTransacao(StatusTransacao.Pendente);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _gateway.Setup(g => g.ProcessarAsync(transacao.Id, transacao.Valor, transacao.Metodo, It.IsAny<CartaoInput>(), default))
            .ReturnsAsync(new CartaoResult(true, "ref-1", "1111", "VISA", null));
        _transacoes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(transacao.Id, Request());

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Aprovado);
        transacao.CartaoUltimos4.Should().Be("1111");
        transacao.CartaoBandeira.Should().Be("VISA");
        transacao.OperadoraRef.Should().Be("ref-1");
    }

    [Fact]
    public async Task Handle_GatewayRecusa_MarcaRecusadoComErro()
    {
        var transacao = CreateTransacao(StatusTransacao.Pendente);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _gateway.Setup(g => g.ProcessarAsync(transacao.Id, transacao.Valor, transacao.Metodo, It.IsAny<CartaoInput>(), default))
            .ReturnsAsync(new CartaoResult(false, null, null, null, "Saldo insuficiente"));
        _transacoes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(transacao.Id, Request());

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Recusado);
        transacao.MensagemErro.Should().Be("Saldo insuficiente");
    }
}

