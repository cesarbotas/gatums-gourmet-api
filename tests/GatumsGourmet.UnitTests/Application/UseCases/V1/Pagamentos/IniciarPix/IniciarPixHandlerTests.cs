using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pagamentos.IniciarPix;

public class IniciarPixHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IPixProvider> _pix = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IniciarPixHandler _handler;

    public IniciarPixHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new IniciarPixHandler(
            _transacoes.Object,
            _pedidos.Object,
            _pix.Object,
            _clock.Object,
            Mock.Of<ILogger<IniciarPixHandler>>());
    }

    private static Pedido CreatePedido(Guid id) => new()
    {
        Id = id,
        UsuarioId = Guid.NewGuid(),
        ClienteNome = "Cliente Teste",
        Total = 50m,
        Data = DateTime.UtcNow,
        StatusAtual = PedidoEtapa.Criado
    };

    [Fact]
    public async Task Handle_PedidoInexistente_RetornaErro()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var result = await _handler.Handle(new IniciarPixRequest(Guid.NewGuid()), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task Handle_TransacaoPendenteExistente_ReaproveitaTransacao()
    {
        var pedidoId = Guid.NewGuid();
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(CreatePedido(pedidoId));
        var existente = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = pedidoId,
            Metodo = MetodoPagamento.Pix,
            Valor = 50m,
            Status = StatusTransacao.Pendente,
            Operadora = "mercadopago",
            PixCopiaCola = "copia-cola-existente"
        };
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedidoId, default)).ReturnsAsync(existente);

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId), null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(existente.Id);
        result.Data.PixCopiaCola.Should().Be("copia-cola-existente");
    }

    [Fact]
    public async Task Handle_NovoPix_GeraComDadosDoPedido()
    {
        var pedidoId = Guid.NewGuid();
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(CreatePedido(pedidoId));
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedidoId, default)).ReturnsAsync((TransacaoPagamento?)null);

        _pix.Setup(p => p.GerarAsync(
            It.IsAny<Guid>(),
            50m,
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Cliente Teste",
            default))
            .ReturnsAsync(new PixResult("0002012658...copia", DateTime.UtcNow.AddMinutes(30), "iVBORw0KGgo...", "mp-123456"));

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId), null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Valor.Should().Be(50m);
        result.Data.Metodo.Should().Be("pix");
        result.Data.PixCopiaCola.Should().Be("0002012658...copia");
        result.Data.PixQrCodeBase64.Should().Be("iVBORw0KGgo...");
        result.Data.Operadora.Should().Be("mercadopago");
        result.Data.OperadoraRef.Should().Be("mp-123456");

        _transacoes.Verify(r => r.AddAsync(It.Is<TransacaoPagamento>(t =>
            t.PedidoId == pedidoId &&
            t.Metodo == MetodoPagamento.Pix &&
            t.OperadoraRef == "mp-123456"), default), Times.Once);
    }
}

