using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoCredito;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pagamentos.ProcessarCartaoCredito;

public class ProcessarCartaoCreditoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ProcessarCartaoCreditoHandler _handler;

    public ProcessarCartaoCreditoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new ProcessarCartaoCreditoHandler(
            _transacoes.Object,
            _pedidos.Object,
            _gateway.Object,
            _clock.Object,
            Mock.Of<ILogger<ProcessarCartaoCreditoHandler>>());
    }

    [Fact]
    public async Task Handle_PedidoInexistente_RetornaErro()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var req = new ProcessarCartaoCreditoRequest(Guid.NewGuid(), 10m, "4111111111111111", "Nome", "12/28", "123");
        var result = await _handler.Handle(req);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task Handle_CartaoAprovado_CriaTransacaoAprovada()
    {
        var pedidoId = Guid.NewGuid();
        var pedido = new Pedido
        {
            Id = pedidoId,
            UsuarioId = Guid.NewGuid(),
            ClienteNome = "Cliente",
            Total = 85.50m,
            Data = DateTime.UtcNow,
            StatusAtual = PedidoEtapa.Criado
        };
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(pedido);

        _gateway.Setup(g => g.ProcessarAsync(
            It.IsAny<Guid>(),
            85.50m,
            MetodoPagamento.CartaoCredito,
            It.IsAny<CartaoInput>(),
            default))
            .ReturnsAsync(new CartaoResult(true, "ref-cartao-123", "1111", "VISA", null));

        var req = new ProcessarCartaoCreditoRequest(pedidoId, null, "4111111111111111", "Nome Titular", "12/28", "123", 2);
        var result = await _handler.Handle(req);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("aprovado");
        result.Data.Metodo.Should().Be("cartaocredito");
        result.Data.CartaoUltimos4.Should().Be("1111");
        result.Data.CartaoBandeira.Should().Be("VISA");

        _transacoes.Verify(r => r.AddAsync(It.Is<TransacaoPagamento>(t =>
            t.Metodo == MetodoPagamento.CartaoCredito &&
            t.Status == StatusTransacao.Aprovado &&
            t.Valor == 85.50m), default), Times.Once);
    }
}

