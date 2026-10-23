using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pagamentos.IniciarPagamento;

public class IniciarPagamentoHandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IPixProvider> _pix = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IniciarPagamentoHandler _handler;

    public IniciarPagamentoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new IniciarPagamentoHandler(
            _transacoes.Object, _pedidos.Object, _pix.Object, _clock.Object,
            Mock.Of<ILogger<IniciarPagamentoHandler>>());
    }

    private static Pedido CreatePedido(Guid id) => new()
    {
        Id = id, UsuarioId = Guid.NewGuid(), ClienteNome = "C", Total = 10m,
        Data = DateTime.UtcNow, StatusAtual = PedidoEtapa.Criado
    };

    [Fact]
    public async Task Handle_PedidoInexistente_RetornaErro()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var result = await _handler.Handle(new IniciarPagamentoRequest(Guid.NewGuid(), MetodoPagamento.Pix, 10m), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task Handle_TransacaoPendenteExistente_RetornaExistente()
    {
        var pedidoId = Guid.NewGuid();
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(CreatePedido(pedidoId));
        var existente = new TransacaoPagamento { Id = Guid.NewGuid(), PedidoId = pedidoId, Metodo = MetodoPagamento.Pix, Valor = 10m, Status = StatusTransacao.Pendente, Operadora = "pix" };
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedidoId, default)).ReturnsAsync(existente);

        var result = await _handler.Handle(new IniciarPagamentoRequest(pedidoId, MetodoPagamento.Pix, 10m), "key-1");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(existente.Id);
        _transacoes.Verify(r => r.AddAsync(It.IsAny<TransacaoPagamento>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_Pix_GeraCopiaColaEPersiste()
    {
        var pedidoId = Guid.NewGuid();
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(CreatePedido(pedidoId));
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedidoId, default)).ReturnsAsync((TransacaoPagamento?)null);
        _pix.Setup(p => p.GerarAsync(It.IsAny<Guid>(), 10m, default))
            .ReturnsAsync(new PixResult("copia-cola-123", new DateTime(2026, 5, 1, 0, 30, 0, DateTimeKind.Utc)));
        TransacaoPagamento? criada = null;
        _transacoes.Setup(r => r.AddAsync(It.IsAny<TransacaoPagamento>(), default))
            .Callback<TransacaoPagamento, CancellationToken>((t, _) => criada = t)
            .Returns(Task.CompletedTask);
        _transacoes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new IniciarPagamentoRequest(pedidoId, MetodoPagamento.Pix, 10m), null);

        result.IsSuccess.Should().BeTrue();
        criada.Should().NotBeNull();
        criada!.Operadora.Should().Be("pix");
        criada.PixCopiaCola.Should().Be("copia-cola-123");
        criada.Status.Should().Be(StatusTransacao.Pendente);
    }

    [Fact]
    public async Task Handle_Cartao_NaoGeraPixEUsaMockGateway()
    {
        var pedidoId = Guid.NewGuid();
        _pedidos.Setup(r => r.GetByIdAsync(pedidoId, default)).ReturnsAsync(CreatePedido(pedidoId));
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedidoId, default)).ReturnsAsync((TransacaoPagamento?)null);
        TransacaoPagamento? criada = null;
        _transacoes.Setup(r => r.AddAsync(It.IsAny<TransacaoPagamento>(), default))
            .Callback<TransacaoPagamento, CancellationToken>((t, _) => criada = t)
            .Returns(Task.CompletedTask);
        _transacoes.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new IniciarPagamentoRequest(pedidoId, MetodoPagamento.CartaoCredito, 25m), null);

        result.IsSuccess.Should().BeTrue();
        criada!.Operadora.Should().Be("mock_gateway");
        criada.PixCopiaCola.Should().BeNull();
        _pix.Verify(p => p.GerarAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), default), Times.Never);
    }
}

