using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.Pagamentos.ValidarPagamentoParaAprovacao;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Pagamentos.ValidarPagamentoParaAprovacao;

public class ValidarPagamentoParaAprovacaoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly ValidarPagamentoParaAprovacaoHandler _handler;

    public ValidarPagamentoParaAprovacaoHandlerTests() => _handler = new ValidarPagamentoParaAprovacaoHandler(_transacoes.Object);

    private static TransacaoPagamento CreateTransacao(StatusTransacao status) => new()
    {
        Id = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Metodo = MetodoPagamento.Pix,
        Valor = 10m, Status = status, Operadora = "pix"
    };

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _transacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((TransacaoPagamento?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não encontrada.");
    }

    [Fact]
    public async Task Handle_NaoAprovada_RetornaErro()
    {
        var transacao = CreateTransacao(StatusTransacao.Pendente);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(transacao.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não está aprovada.");
    }

    [Fact]
    public async Task Handle_Aprovada_RetornaTransacao()
    {
        var transacao = CreateTransacao(StatusTransacao.Aprovado);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(transacao.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("aprovado");
    }
}

