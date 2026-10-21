using FluentAssertions;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Application.UseCases.Relatorios.GetVendasResumo;
using GatumsGourmet.Domain.Entities.Vendas;
using Moq;
using Xunit;

namespace GatumsGourmet.UnitTests.Application.UseCases.Relatorios.GetVendasResumo;

public class GetVendasResumoHandlerTests
{
    private readonly Mock<IVendaRepository> _vendas = new();
    private readonly GetVendasResumoHandler _handler;

    public GetVendasResumoHandlerTests() => _handler = new GetVendasResumoHandler(_vendas.Object);

    [Fact]
    public async Task Handle_SemVendas_RetornaZeros()
    {
        _vendas.Setup(r => r.GetUltimos30DiasAsync(default)).ReturnsAsync(new List<Venda>());

        var result = await _handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data!.Faturamento.Should().Be(0m);
        result.Data.TotalPedidos.Should().Be(0);
        result.Data.TicketMedio.Should().Be(0m);
        result.Data.TopProdutos.Should().BeEmpty();
        result.Data.SerieDiaria.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ComVendas_CalculaFaturamentoTicketETop()
    {
        var pedido1 = Guid.NewGuid();
        var pedido2 = Guid.NewGuid();
        var prodA = Guid.NewGuid();
        var prodB = Guid.NewGuid();
        var dia = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var vendas = new List<Venda>
        {
            new() { Id = Guid.NewGuid(), PedidoId = pedido1, ProdutoId = prodA, ProdutoNome = "A", Quantidade = 2, Total = 100m, Data = dia },
            new() { Id = Guid.NewGuid(), PedidoId = pedido1, ProdutoId = prodB, ProdutoNome = "B", Quantidade = 1, Total = 50m, Data = dia },
            new() { Id = Guid.NewGuid(), PedidoId = pedido2, ProdutoId = prodA, ProdutoNome = "A", Quantidade = 3, Total = 150m, Data = dia.AddDays(1) }
        };
        _vendas.Setup(r => r.GetUltimos30DiasAsync(default)).ReturnsAsync(vendas);

        var result = await _handler.Handle();

        result.IsSuccess.Should().BeTrue();
        result.Data!.Faturamento.Should().Be(300m);
        result.Data.TotalPedidos.Should().Be(2); // pedido1 e pedido2
        result.Data.TicketMedio.Should().Be(150m); // 300 / 2
        result.Data.TopProdutos.Should().HaveCount(2);
        result.Data.TopProdutos[0].ProdutoId.Should().Be(prodA); // maior Total (250)
        result.Data.TopProdutos[0].Total.Should().Be(250m);
        result.Data.SerieDiaria.Should().HaveCount(2);
        result.Data.SerieDiaria[0].Data.Should().Be("2026-01-10");
    }
}
