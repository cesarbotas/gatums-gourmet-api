using System.Net;
using FluentAssertions;
using GatumsGourmet.IntegrationTests.Factory;
using Xunit;

namespace GatumsGourmet.IntegrationTests.Api.Paginacao;

/// <summary>
/// Regressão: os GET de listagem usam [AsParameters] com as propriedades de
/// paginação. Se elas forem tipos-valor não-anuláveis (int), o binding do
/// Minimal API as trata como OBRIGATÓRIAS e estoura
/// BadHttpRequestException ("Required parameter 'int Pagina'...") quando o
/// cliente chama o endpoint SEM ?pagina=&amp;tamanho= — foi o que quebrou o
/// Newman no CI. A paginação tem defaults; chamar sem os params deve dar 200.
/// </summary>
[Collection("Integration")]
public class ListagemSemQueryParamsTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ListagemSemQueryParamsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Produtos_SemQueryParams_RetornaOk()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/produtos");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Categorias_SemQueryParams_RetornaOk()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/categorias");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Clientes_SemQueryParams_AdminRetornaOk()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.GetAsync("/api/v1/clientes");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Pedidos_SemQueryParams_AdminRetornaOk()
    {
        var (admin, _) = await _factory.CreateAdminClientAsync();

        var resp = await admin.GetAsync("/api/v1/pedidos");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
