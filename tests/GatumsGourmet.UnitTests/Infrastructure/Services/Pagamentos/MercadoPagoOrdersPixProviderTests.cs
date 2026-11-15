using System.Globalization;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Infrastructure.Services.Pagamentos;
using Xunit;

namespace GatumsGourmet.UnitTests.Infrastructure.Services.Pagamentos;

public class MercadoPagoOrdersPixProviderTests
{
    private readonly MercadoPagoOrdersPixProviderFixture _fixture = new();
    private static readonly DateTime Agora = MercadoPagoOrdersPixProviderFixture.Agora;

    private static PixOrderRequest NovaRequest(Guid? id = null, decimal valor = 50m) =>
        new(id ?? Guid.NewGuid(), valor, "Pedido teste", "cliente@exemplo.com", "Maria");

    // ── Contrato do body e headers ───────────────────────────────────────────

    [Fact]
    public async Task CriarOrderAsync_EnviaBodyEHeadersDoContrato()
    {
        var (provider, handler) = _fixture.CriarProvider(HttpStatusCode.Created, _fixture.SuccessJson);
        var id = Guid.NewGuid();

        await provider.CriarOrderAsync(NovaRequest(id));

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.AbsolutePath.Should().EndWith("v1/orders");
        handler.Request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
        handler.Request.Headers.GetValues("X-Idempotency-Key").Should().ContainSingle()
            .Which.Should().Be(id.ToString());

        using var doc = JsonDocument.Parse(handler.RequestBody!);
        var root = doc.RootElement;
        root.GetProperty("type").GetString().Should().Be("online");
        root.GetProperty("processing_mode").GetString().Should().Be("automatic");
        root.GetProperty("external_reference").GetString().Should().Be(id.ToString());
        root.GetProperty("total_amount").GetString().Should().Be("50.00");

        var pagamento = root.GetProperty("transactions").GetProperty("payments")[0];
        pagamento.GetProperty("amount").GetString().Should().Be("50.00");
        pagamento.GetProperty("payment_method").GetProperty("id").GetString().Should().Be("pix");
        pagamento.GetProperty("payment_method").GetProperty("type").GetString().Should().Be("bank_transfer");
        root.GetProperty("payer").GetProperty("email").GetString().Should().Be("cliente@exemplo.com");
        // config.payment.expiration_time is controlled server-side via MercadoPago:PixExpirationMinutes
        // — the test verifies the property is NOT sent in the top-level body (not duplicated)
        root.TryGetProperty("expiration_time", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CriarOrderAsync_ValorComCulturaPtBr_UsaPonto()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            var (provider, handler) = _fixture.CriarProvider(HttpStatusCode.Created, _fixture.SuccessJson);

            await provider.CriarOrderAsync(NovaRequest(valor: 50.5m));

            var json = JsonDocument.Parse(handler.RequestBody!);
            json.RootElement.GetProperty("total_amount").GetString().Should().Be("50.50");
            json.RootElement.GetProperty("transactions").GetProperty("payments")[0]
                .GetProperty("amount").GetString().Should().Be("50.50");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ── Mapeamento da resposta ───────────────────────────────────────────────

    [Fact]
    public async Task CriarOrderAsync_RespostaDeSucesso_MapeiaOrderIdQrCodeETicket()
    {
        var (provider, _) = _fixture.CriarProvider(HttpStatusCode.Created, _fixture.SuccessJson);

        var result = await provider.CriarOrderAsync(NovaRequest());

        result.OrderId.Should().Be("ORD01HRYFWNYRE1MR1E60MW3X0T2P");
        result.CopiaCola.Should().Be("00020126580014br.gov.bcb.pix0136test-qr");
        result.QrCodeBase64.Should().Be("iVBORw0KGgoAAAANSUhEUg==");
        result.TicketUrl.Should().Be("https://www.mercadopago.com.br/payments/123/ticket?caller_id=1");
        result.ExpiraEm.Should().Be(Agora.AddMinutes(30));
    }

    [Fact]
    public async Task CriarOrderAsync_RespostaSemQrCode_LancaPixOrdersProviderException()
    {
        const string semQr = """
            { "id": "ORD1", "transactions": { "payments": [
              { "id": "PAY1", "payment_method": { "id": "pix", "type": "bank_transfer" } }
            ] } }
            """;
        var (provider, _) = _fixture.CriarProvider(HttpStatusCode.Created, semQr);

        await Assert.ThrowsAsync<PixOrdersProviderException>(
            () => provider.CriarOrderAsync(NovaRequest()));
    }

    // ── Erros HTTP ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CriarOrderAsync_Http400_LancaExcecaoComStatusEMensagemDoErrors()
    {
        const string erro = """
            { "errors": [ { "code": "invalid_email", "message": "Payer email is invalid" } ] }
            """;
        var (provider, _) = _fixture.CriarProvider(HttpStatusCode.BadRequest, erro);

        var ex = await Assert.ThrowsAsync<PixOrdersProviderException>(
            () => provider.CriarOrderAsync(NovaRequest()));
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Contain("invalid_email").And.Contain("Payer email is invalid");
    }

    // ── ConsultarOrder ───────────────────────────────────────────────────────

    [Fact]
    public async Task ConsultarOrderAsync_Sucesso_RetornaStatusEStatusDetail()
    {
        var (provider, handler) = _fixture.CriarProvider(HttpStatusCode.OK,
            """{ "id": "ORD1", "status": "processed", "status_detail": "accredited" }""");

        var status = await provider.ConsultarOrderAsync("ORD1");

        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri!.AbsolutePath.Should().EndWith("v1/orders/ORD1");
        status.Should().Be(new PixOrderStatus("processed", "accredited"));
    }

    [Fact]
    public async Task ConsultarOrderAsync_404_RetornaNull()
    {
        var (provider, _) = _fixture.CriarProvider(HttpStatusCode.NotFound,
            """{ "errors": [ { "code": "not_found", "message": "x" } ] }""");

        var status = await provider.ConsultarOrderAsync("ORD404");

        status.Should().BeNull();
    }

    // ── Sem token configurado ────────────────────────────────────────────────

    [Fact]
    public async Task CriarOrderAsync_SemAccessToken_LancaInvalidOperationException()
    {
        var (provider, _) = _fixture.CriarProvider(HttpStatusCode.Created, _fixture.SuccessJson, token: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.CriarOrderAsync(NovaRequest()));
    }
}
