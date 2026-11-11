using FluentAssertions;
using GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;
using GatumsGourmet.Application.UseCases.V2.Pagamentos.IniciarPix;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.V2.Pagamentos.IniciarPix;

public class IniciarPixV2HandlerTests
{
    private readonly Mock<ITransacaoRepository> _transacoes = new();
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IPixOrdersProvider> _provider = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IniciarPixV2Handler _handler;

    public IniciarPixV2HandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new IniciarPixV2Handler(
            _transacoes.Object,
            _pedidos.Object,
            _usuarios.Object,
            _provider.Object,
            _clock.Object,
            Mock.Of<ILogger<IniciarPixV2Handler>>());
    }

    private static Pedido CreatePedido(Guid id, Guid? usuarioId = null) => new()
    {
        Id = id,
        UsuarioId = usuarioId ?? Guid.NewGuid(),
        ClienteNome = "Cliente Teste",
        Total = 50m,
        Data = DateTime.UtcNow,
        StatusAtual = PedidoEtapa.Criado
    };

    private void SetupPedido(Pedido pedido, string? emailUsuario)
    {
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedido.Id, default)).ReturnsAsync((TransacaoPagamento?)null);
        _usuarios.Setup(r => r.GetByIdAsync(pedido.UsuarioId, default)).ReturnsAsync(
            emailUsuario is null ? null : new Usuario { Id = pedido.UsuarioId, Email = emailUsuario });
    }

    private static PixOrderResult OrderResult() =>
        new("ORD01TEST", "0002012658...copia", "iVBORw0KGgo...", "https://example.test/ticket", new DateTime(2026, 5, 1, 0, 30, 0, DateTimeKind.Utc));

    [Fact]
    public async Task PedidoInexistente_Fail()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var result = await _handler.Handle(new IniciarPixRequest(Guid.NewGuid()), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task CriaOrder_PersisteQrExpiracaoOperadoraRefEOperadora()
    {
        var pedidoId = Guid.NewGuid();
        SetupPedido(CreatePedido(pedidoId), "usuario@example.test");
        _provider.Setup(p => p.CriarOrderAsync(It.IsAny<PixOrderRequest>(), default)).ReturnsAsync(OrderResult());

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId, PayerEmail: "pagador@example.test"), null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Operadora.Should().Be("mercadopago-orders");
        result.Data.OperadoraRef.Should().Be("ORD01TEST");
        result.Data.PixCopiaCola.Should().Be("0002012658...copia");
        result.Data.PixQrCodeBase64.Should().Be("iVBORw0KGgo...");
        result.Data.Valor.Should().Be(50m);

        _transacoes.Verify(r => r.AddAsync(It.Is<TransacaoPagamento>(t =>
            t.PedidoId == pedidoId &&
            t.Metodo == MetodoPagamento.Pix &&
            t.Status == StatusTransacao.Pendente &&
            t.Operadora == "mercadopago-orders" &&
            t.OperadoraRef == "ORD01TEST" &&
            t.PixExpiraEm == new DateTime(2026, 5, 1, 0, 30, 0, DateTimeKind.Utc)), default), Times.Once);
        _transacoes.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _provider.Verify(p => p.CriarOrderAsync(It.Is<PixOrderRequest>(o =>
            o.PayerEmail == "pagador@example.test" &&
            o.Valor == 50m &&
            o.PayerNome == "Cliente Teste"), default), Times.Once);
    }

    [Fact]
    public async Task SemEmailNoRequest_UsaEmailDoUsuarioDoPedido()
    {
        var pedidoId = Guid.NewGuid();
        SetupPedido(CreatePedido(pedidoId), "usuario@example.test");
        _provider.Setup(p => p.CriarOrderAsync(It.IsAny<PixOrderRequest>(), default)).ReturnsAsync(OrderResult());

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId), null);

        result.IsSuccess.Should().BeTrue();
        _provider.Verify(p => p.CriarOrderAsync(It.Is<PixOrderRequest>(o => o.PayerEmail == "usuario@example.test"), default), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SemEmailEmLugarNenhum_Fail_NaoChamaProvider_NaoPersiste(string? emailUsuario)
    {
        var pedidoId = Guid.NewGuid();
        SetupPedido(CreatePedido(pedidoId), emailUsuario);

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId, PayerEmail: "  "), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
        _provider.Verify(p => p.CriarOrderAsync(It.IsAny<PixOrderRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _transacoes.Verify(r => r.AddAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _transacoes.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransacaoPendenteV2Existente_Reaproveita_NaoChamaProvider()
    {
        var pedidoId = Guid.NewGuid();
        SetupPedido(CreatePedido(pedidoId), "usuario@example.test");
        var existente = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = pedidoId,
            Metodo = MetodoPagamento.Pix,
            Valor = 50m,
            Status = StatusTransacao.Pendente,
            Operadora = "mercadopago-orders",
            OperadoraRef = "ORDEXISTENTE",
            PixCopiaCola = "copia-cola-existente"
        };
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedidoId, default)).ReturnsAsync(existente);

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId), null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(existente.Id);
        result.Data.PixCopiaCola.Should().Be("copia-cola-existente");
        _provider.Verify(p => p.CriarOrderAsync(It.IsAny<PixOrderRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _transacoes.Verify(r => r.AddAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransacaoPendenteV1_NaoReaproveita_CriaNovaOrder()
    {
        var pedidoId = Guid.NewGuid();
        SetupPedido(CreatePedido(pedidoId), "usuario@example.test");
        var v1 = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = pedidoId,
            Metodo = MetodoPagamento.Pix,
            Valor = 50m,
            Status = StatusTransacao.Pendente,
            Operadora = "mercadopago",
            OperadoraRef = "123456789",
            PixCopiaCola = "copia-cola-v1"
        };
        _transacoes.Setup(r => r.GetByPedidoIdAsync(pedidoId, default)).ReturnsAsync(v1);
        _provider.Setup(p => p.CriarOrderAsync(It.IsAny<PixOrderRequest>(), default)).ReturnsAsync(OrderResult());

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId), null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().NotBe(v1.Id);
        result.Data.Operadora.Should().Be("mercadopago-orders");
        result.Data.OperadoraRef.Should().Be("ORD01TEST");
        _provider.Verify(p => p.CriarOrderAsync(It.IsAny<PixOrderRequest>(), default), Times.Once);
        _transacoes.Verify(r => r.AddAsync(It.IsAny<TransacaoPagamento>(), default), Times.Once);
    }

    [Fact]
    public async Task ProviderLancaPixOrdersProviderException_Fail_ComMensagem_NaoPersiste()
    {
        var pedidoId = Guid.NewGuid();
        SetupPedido(CreatePedido(pedidoId), "usuario@example.test");
        _provider.Setup(p => p.CriarOrderAsync(It.IsAny<PixOrderRequest>(), default))
            .ThrowsAsync(new PixOrdersProviderException("Resposta do Mercado Pago sem QR Code.", 200));

        var result = await _handler.Handle(new IniciarPixRequest(pedidoId), null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Resposta do Mercado Pago sem QR Code.");
        _transacoes.Verify(r => r.AddAsync(It.IsAny<TransacaoPagamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _transacoes.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
