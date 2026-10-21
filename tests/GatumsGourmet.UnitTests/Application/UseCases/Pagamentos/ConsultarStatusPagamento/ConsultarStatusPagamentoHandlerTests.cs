using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Pagamentos.ConsultarStatusPagamento;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Pagamentos.ConsultarStatusPagamento;

public class ConsultarStatusPagamentoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly ConsultarStatusPagamentoHandler _handler;

    public ConsultarStatusPagamentoHandlerTests() => _handler = new ConsultarStatusPagamentoHandler(_transacoes.Object);

    [Fact]
    public async Task Handle_Inexistente_RetornaErro()
    {
        _transacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((TransacaoPagamento?)null);

        var result = await _handler.Handle(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não encontrada.");
    }

    [Fact]
    public async Task Handle_Existente_RetornaTransacaoComStatusLowercase()
    {
        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(), PedidoId = Guid.NewGuid(), Metodo = MetodoPagamento.Pix,
            Valor = 10m, Status = StatusTransacao.Pendente, Operadora = "pix"
        };
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(transacao.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(transacao.Id);
        result.Data.Status.Should().Be("pendente");
        result.Data.Metodo.Should().Be("pix");
    }
}
