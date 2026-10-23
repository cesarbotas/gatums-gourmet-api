using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ConfirmarPagamentoPix;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pagamentos.ConfirmarPagamentoPix;

public class ConfirmarPagamentoPixHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPixProvider> _pix = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ConfirmarPagamentoPixHandler _handler;

    public ConfirmarPagamentoPixHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new ConfirmarPagamentoPixHandler(
            _transacoes.Object, _pix.Object, _clock.Object,
            Mock.Of<ILogger<ConfirmarPagamentoPixHandler>>());
    }

    private static TransacaoPagamento CreateTransacao(MetodoPagamento metodo, StatusTransacao status) => new()
    {
        Id = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Metodo = metodo,
        Valor = 10m, Status = status, Operadora = "pix"
    };

    [Fact]
    public async Task Handle_TransacaoInexistente_RetornaErro()
    {
        _transacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((TransacaoPagamento?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não encontrada.");
    }

    [Fact]
    public async Task Handle_NaoEhPix_RetornaErro()
    {
        var transacao = CreateTransacao(MetodoPagamento.CartaoCredito, StatusTransacao.Pendente);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(transacao.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não é Pix.");
    }

    [Fact]
    public async Task Handle_JaProcessada_RetornaErro()
    {
        var transacao = CreateTransacao(MetodoPagamento.Pix, StatusTransacao.Aprovado);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(transacao.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação já processada.");
    }

    [Fact]
    public async Task Handle_PixConfirmado_MarcaAprovado()
    {
        var transacao = CreateTransacao(MetodoPagamento.Pix, StatusTransacao.Pendente);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _pix.Setup(p => p.ConfirmarAsync(transacao.Id, default)).ReturnsAsync(true);
        _transacoes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(transacao.Id);

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Aprovado);
    }

    [Fact]
    public async Task Handle_PixNaoConfirmado_PermaneceePendente()
    {
        var transacao = CreateTransacao(MetodoPagamento.Pix, StatusTransacao.Pendente);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _pix.Setup(p => p.ConfirmarAsync(transacao.Id, default)).ReturnsAsync(false);
        _transacoes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(transacao.Id);

        result.IsSuccess.Should().BeTrue();
        transacao.Status.Should().Be(StatusTransacao.Pendente);
    }
}

