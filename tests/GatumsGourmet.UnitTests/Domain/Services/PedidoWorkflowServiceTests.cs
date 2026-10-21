using FluentAssertions;
using GatumsGourmet.Domain.Enums.Pedidos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Services;
using Xunit;

namespace GatumsGourmet.UnitTests.Domain.Services;

public class PedidoWorkflowServiceTests
{
    private readonly PedidoWorkflowService _service = new();

    [Theory]
    [InlineData(PedidoEtapa.Criado, PedidoEtapa.Aprovado, PedidoActor.Admin)]
    [InlineData(PedidoEtapa.Aprovado, PedidoEtapa.Pagamento, PedidoActor.Cliente)]
    [InlineData(PedidoEtapa.Pagamento, PedidoEtapa.PagamentoAprovado, PedidoActor.Admin)]
    [InlineData(PedidoEtapa.PagamentoAprovado, PedidoEtapa.EnvioPendente, PedidoActor.Admin)]
    [InlineData(PedidoEtapa.EnvioPendente, PedidoEtapa.Entregue, PedidoActor.Admin)]
    [InlineData(PedidoEtapa.Entregue, PedidoEtapa.Finalizado, PedidoActor.Admin)]
    public void ValidarTransicao_TransicoesValidas_NaoLancam(PedidoEtapa atual, PedidoEtapa proxima, PedidoActor actor)
    {
        var act = () => _service.ValidarTransicao(atual, proxima, actor);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidarTransicao_EtapaFinalizada_NaoPermiteNenhumaTransicao()
    {
        // Finalizado está mapeado com lista vazia de transições, então qualquer
        // próxima etapa cai em "não é permitida".
        var act = () => _service.ValidarTransicao(PedidoEtapa.Finalizado, PedidoEtapa.Criado, PedidoActor.Admin);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*não é permitida*");
    }

    [Fact]
    public void ValidarTransicao_TransicaoNaoPermitida_Lanca()
    {
        var act = () => _service.ValidarTransicao(PedidoEtapa.Criado, PedidoEtapa.Finalizado, PedidoActor.Admin);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*não é permitida*");
    }

    [Fact]
    public void ValidarTransicao_ActorErrado_Lanca()
    {
        // Criado -> Aprovado exige Admin; passar Cliente deve falhar.
        var act = () => _service.ValidarTransicao(PedidoEtapa.Criado, PedidoEtapa.Aprovado, PedidoActor.Cliente);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Apenas*pode avançar*");
    }
}
