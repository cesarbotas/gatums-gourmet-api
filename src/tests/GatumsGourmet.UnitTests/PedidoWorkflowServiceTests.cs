using FluentAssertions;
using GatumsGourmet.Domain.Enums;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Services;

namespace GatumsGourmet.UnitTests;

public class PedidoWorkflowServiceTests
{
    private readonly PedidoWorkflowService _svc = new();

    [Theory]
    [InlineData(PedidoEtapa.Criado, PedidoEtapa.Aprovado, PedidoActor.Admin)]
    [InlineData(PedidoEtapa.Aprovado, PedidoEtapa.Pagamento, PedidoActor.Cliente)]
    [InlineData(PedidoEtapa.Pagamento, PedidoEtapa.PagamentoAprovado, PedidoActor.Admin)]
    [InlineData(PedidoEtapa.EnvioPendente, PedidoEtapa.Entregue, PedidoActor.Admin)]
    public void ValidarTransicao_TransicaoValida_NaoLancaExcecao(PedidoEtapa atual, PedidoEtapa proxima, PedidoActor actor)
    {
        var act = () => _svc.ValidarTransicao(atual, proxima, actor);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidarTransicao_AtorErrado_LancaBusinessRuleException()
    {
        var act = () => _svc.ValidarTransicao(PedidoEtapa.Criado, PedidoEtapa.Aprovado, PedidoActor.Cliente);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ValidarTransicao_EtapaInvalida_LancaBusinessRuleException()
    {
        var act = () => _svc.ValidarTransicao(PedidoEtapa.Criado, PedidoEtapa.Finalizado, PedidoActor.Admin);
        act.Should().Throw<BusinessRuleException>();
    }
}
