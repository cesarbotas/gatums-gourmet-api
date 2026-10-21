using FluentAssertions;
using GatumsGourmet.Application.UseCases.Pagamentos.ListOpcoesPagamento;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Pagamentos.ListOpcoesPagamento;

public class ListOpcoesPagamentoHandlerTests
{
    private readonly ListOpcoesPagamentoHandler _handler = new();

    [Fact]
    public async Task Handle_RetornaOpcoesFixas()
    {
        var result = await _handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(3);
        result.Data.Should().Contain(o => o.Metodo == "pix" && o.Disponivel);
        result.Data.Should().Contain(o => o.Metodo == "cartao_credito");
        result.Data.Should().Contain(o => o.Metodo == "cartao_debito");
    }
}
