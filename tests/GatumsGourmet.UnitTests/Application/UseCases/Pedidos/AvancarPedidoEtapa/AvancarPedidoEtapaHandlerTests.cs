using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Pedidos.AvancarPedidoEtapa;
using GatumsGourmet.Domain.Entities.Pedidos;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Interfaces;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Pedidos.AvancarPedidoEtapa;

public class AvancarPedidoEtapaHandlerTests
{
    private readonly Mock<IPedidoRepository> _pedidos = new();
    private readonly Mock<IPedidoWorkflowService> _workflow = new();
    private readonly Mock<IClock> _clock = new();
    private readonly AvancarPedidoEtapaHandler _handler;

    public AvancarPedidoEtapaHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        _handler = new AvancarPedidoEtapaHandler(_pedidos.Object, _workflow.Object, _clock.Object);
    }

    private static Pedido CreatePedido(PedidoEtapa etapa) => new()
    {
        Id = Guid.NewGuid(), UsuarioId = Guid.NewGuid(), ClienteNome = "C",
        Total = 10m, Data = DateTime.UtcNow, StatusAtual = etapa
    };

    [Fact]
    public async Task Handle_PedidoInexistente_RetornaErro()
    {
        _pedidos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Pedido?)null);

        var result = await _handler.Handle(Guid.NewGuid(), new AvancarPedidoEtapaRequest(PedidoEtapa.Aprovado, null, null), PedidoActor.Admin);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Pedido não encontrado.");
    }

    [Fact]
    public async Task Handle_EtapaEntregueSemEntregaTipo_LancaBusinessRule()
    {
        var pedido = CreatePedido(PedidoEtapa.EnvioPendente);
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);

        var act = () => _handler.Handle(pedido.Id, new AvancarPedidoEtapaRequest(PedidoEtapa.Entregue, null, null), PedidoActor.Admin);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*entregue*");
        _workflow.Verify(w => w.ValidarTransicao(It.IsAny<PedidoEtapa>(), It.IsAny<PedidoEtapa>(), It.IsAny<PedidoActor>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TransicaoValida_AvancaEtapaEAdicionaTimeline()
    {
        var pedido = CreatePedido(PedidoEtapa.Criado);
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        _pedidos.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(pedido.Id, new AvancarPedidoEtapaRequest(PedidoEtapa.Aprovado, "ok", null), PedidoActor.Admin);

        result.IsSuccess.Should().BeTrue();
        pedido.StatusAtual.Should().Be(PedidoEtapa.Aprovado);
        pedido.Timeline.Should().ContainSingle(t => t.Etapa == PedidoEtapa.Aprovado && t.Observacao == "ok");
        _workflow.Verify(w => w.ValidarTransicao(PedidoEtapa.Criado, PedidoEtapa.Aprovado, PedidoActor.Admin), Times.Once);
        _pedidos.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowRejeitaTransicao_PropagaExcecao()
    {
        var pedido = CreatePedido(PedidoEtapa.Criado);
        _pedidos.Setup(r => r.GetByIdAsync(pedido.Id, default)).ReturnsAsync(pedido);
        _workflow.Setup(w => w.ValidarTransicao(PedidoEtapa.Criado, PedidoEtapa.Finalizado, PedidoActor.Admin))
            .Throws(new BusinessRuleException("Transição inválida."));

        var act = () => _handler.Handle(pedido.Id, new AvancarPedidoEtapaRequest(PedidoEtapa.Finalizado, null, null), PedidoActor.Admin);

        await act.Should().ThrowAsync<BusinessRuleException>();
        _pedidos.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
