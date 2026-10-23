using FluentAssertions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Application.UseCases.V1.Pedidos.ConcluirPagamentoPedido;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V1.Pedidos.ConcluirPagamentoPedido;

public class ConcluirPagamentoPedidoHandlerTests
{
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ConcluirPagamentoPedidoHandler _handler;

    public ConcluirPagamentoPedidoHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new ConcluirPagamentoPedidoHandler(
            _pedidos.Object, _transacoes.Object, _clock.Object,
            Mock.Of<ILogger<ConcluirPagamentoPedidoHandler>>());
    }

    private static Pedido CreatePedido() => new()
    {
        Id = Guid.NewGuid(), UsuarioId = Guid.NewGuid(), ClienteNome = "C",
        Total = 10m, Data = DateTime.UtcNow, StatusAtual = PedidoEtapa.Criado
    };

    private static TransacaoPagamento CreateTransacao(Guid pedidoId, StatusTransacao status, MetodoPagamento metodo) => new()
    {
        Id = Guid.NewGuid(), PedidoId = pedidoId, Metodo = metodo, Valor = 10m,
        Status = status, Operadora = "mock", CartaoBandeira = "VISA", CartaoUltimos4 = "1234"
    };

    [Fact]
    public async Task Handle_PedidoInexistente_RetornaErro()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var result = await _handler.Handle(Guid.NewGuid(), new ConcluirPagamentoPedidoRequest(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task Handle_TransacaoDeOutroPedido_RetornaInvalida()
    {
        var pedido = CreatePedido();
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        var transacao = CreateTransacao(Guid.NewGuid(), StatusTransacao.Aprovado, MetodoPagamento.Pix);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(pedido.Id, new ConcluirPagamentoPedidoRequest(transacao.Id));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação inválida.");
    }

    [Fact]
    public async Task Handle_TransacaoNaoAprovada_RetornaErro()
    {
        var pedido = CreatePedido();
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        var transacao = CreateTransacao(pedido.Id, StatusTransacao.Pendente, MetodoPagamento.Pix);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);

        var result = await _handler.Handle(pedido.Id, new ConcluirPagamentoPedidoRequest(transacao.Id));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transação não aprovada.");
    }

    [Fact]
    public async Task Handle_Pix_ConcluiComResumoPix()
    {
        var pedido = CreatePedido();
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        var transacao = CreateTransacao(pedido.Id, StatusTransacao.Aprovado, MetodoPagamento.Pix);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _pedidos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(pedido.Id, new ConcluirPagamentoPedidoRequest(transacao.Id));

        result.IsSuccess.Should().BeTrue();
        pedido.StatusAtual.Should().Be(PedidoEtapa.Pagamento);
        pedido.Pagamento.Should().NotBeNull();
        pedido.Pagamento!.Resumo.Should().Be("Pix");
        pedido.Timeline.Should().ContainSingle(t => t.Etapa == PedidoEtapa.Pagamento);
    }

    [Fact]
    public async Task Handle_Cartao_ConcluiComResumoCartao()
    {
        var pedido = CreatePedido();
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        var transacao = CreateTransacao(pedido.Id, StatusTransacao.Aprovado, MetodoPagamento.CartaoCredito);
        _transacoes.Setup(r => r.GetByIdAsync(transacao.Id, default)).ReturnsAsync(transacao);
        _pedidos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(pedido.Id, new ConcluirPagamentoPedidoRequest(transacao.Id));

        result.IsSuccess.Should().BeTrue();
        pedido.Pagamento!.Resumo.Should().Be("VISA **** 1234");
    }
}

