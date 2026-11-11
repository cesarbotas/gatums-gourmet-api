using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;

public class ProcessarCartaoDebitoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ProcessarCartaoDebitoHandler _handler;

    public ProcessarCartaoDebitoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new ProcessarCartaoDebitoHandler(
            _transacoes.Object,
            _pedidos.Object,
            _gateway.Object,
            _clock.Object,
            Mock.Of<ILogger<ProcessarCartaoDebitoHandler>>());
    }

    [Fact]
    public async Task Handle_PedidoInexistente_RetornaErro()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var req = new ProcessarCartaoDebitoRequest(Guid.NewGuid(), 20m, "4111111111111111", "Nome", "12/28", "123");
        var result = await _handler.Handle(req);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task Handle_CartaoDebitoAprovado_CriaTransacao()
    {
        var pedidoId = Guid.NewGuid();
        var pedido = new Pedido
        {
            Id = pedidoId,
            UsuarioId = Guid.NewGuid(),
            ClienteNome = "Cliente",
            Total = 40m,
            Data = DateTime.UtcNow,
            StatusAtual = PedidoEtapa.Criado
        };
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(pedido);

        _gateway.Setup(g => g.ProcessarAsync(
            It.IsAny<Guid>(),
            40m,
            MetodoPagamento.CartaoDebito,
            It.IsAny<CartaoInput>(),
            default))
            .ReturnsAsync(new CartaoResult(true, "ref-debito-456", "2222", "MASTERCARD", null));

        var req = new ProcessarCartaoDebitoRequest(pedidoId, null, "4111111111111111", "Nome Titular", "12/28", "123");
        var result = await _handler.Handle(req);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("aprovado");
        result.Data.Metodo.Should().Be("cartaodebito");
        result.Data.CartaoUltimos4.Should().Be("2222");

        _transacoes.Verify(r => r.AddAsync(It.Is<TransacaoPagamento>(t =>
            t.Metodo == MetodoPagamento.CartaoDebito &&
            t.Status == StatusTransacao.Aprovado &&
            t.Valor == 40m), default), Times.Once);
    }
}

