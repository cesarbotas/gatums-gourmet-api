using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Pedidos.AvancarPedidoEtapa;
using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Interfaces;
using GatumsGourmet.Domain.Services;
using Moq;

namespace GatumsGourmet.UnitTests;

public class AvancarPedidoEtapaHandlerTests
{
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IClock> _clock = new();
    private readonly AvancarPedidoEtapaHandler _handler;

    public AvancarPedidoEtapaHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);
        _handler = new AvancarPedidoEtapaHandler(_pedidos.Object, new PedidoWorkflowService(), _clock.Object);
    }

    [Fact]
    public async Task Handle_PedidoNaoEncontrado_RetornaErro()
    {
        _pedidos.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var result = await _handler.Handle(Guid.NewGuid(), new AvancarPedidoEtapaRequest(PedidoEtapa.Aprovado, null, null), PedidoActor.Admin, default);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TransicaoValida_AtualizaEtapa()
    {
        var pedido = new Pedido { Id = Guid.NewGuid(), StatusAtual = PedidoEtapa.Criado, Timeline = [] };
        _pedidos.Setup(p => p.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        _pedidos.Setup(p => p.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(pedido.Id, new AvancarPedidoEtapaRequest(PedidoEtapa.Aprovado, null, null), PedidoActor.Admin, default);

        result.IsSuccess.Should().BeTrue();
        pedido.StatusAtual.Should().Be(PedidoEtapa.Aprovado);
    }

    [Fact]
    public async Task Handle_TransicaoInvalida_LancaExcecao()
    {
        var pedido = new Pedido { Id = Guid.NewGuid(), StatusAtual = PedidoEtapa.Criado, Timeline = [] };
        _pedidos.Setup(p => p.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);

        var act = () => _handler.Handle(pedido.Id, new AvancarPedidoEtapaRequest(PedidoEtapa.Finalizado, null, null), PedidoActor.Admin, default);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_EntregaSemTipo_LancaExcecao()
    {
        var pedido = new Pedido { Id = Guid.NewGuid(), StatusAtual = PedidoEtapa.EnvioPendente, Timeline = [] };
        _pedidos.Setup(p => p.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);

        var act = () => _handler.Handle(pedido.Id, new AvancarPedidoEtapaRequest(PedidoEtapa.Entregue, null, null), PedidoActor.Admin, default);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*EntregaTipo*");
    }
}
