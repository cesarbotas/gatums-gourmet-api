using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Infrastructure.Services.Pagamentos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GatumsGourmet.UnitTests.Infrastructure.Services.Pagamentos;

public class MercadoPagoOrdersPixProviderTests
{
    private const string SuccessJson = """
    {
      "id": "ORD01HRYFWNYRE1MR1E60MW3X0T2P",
      "type": "online",
      "status": "action_required",
      "status_detail": "waiting_transfer",
      "transactions": {
        "payments": [
          {
            "id": "PAY01HRYFWNYRE1MR1E60MW3X0T2P",
            "status": "action_required",
            "payment_method": {
              "id": "pix",
              "type": "bank_transfer",
              "qr_code": "00020126580014br.gov.bcb.pix0136test-qr",
              "qr_code_base64": "iVBORw0KGgoAAAANSUhEUg==",
              "ticket_url": "https://www.mercadopago.com.br/payments/123/ticket?caller_id=1"
            }
          }
        ]
      }
    }
    """;

    private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    private static readonly DateTime Agora = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    private static (MercadoPagoOrdersPixProvider Provider, FakeHandler Handler) CriarProvider(
        HttpStatusCode status, string body, string? token = "test-token")
    {
        var handler = new FakeHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mercadopago.test/") };
        var values = new Dictionary<string, string?> { ["MercadoPago:AccessToken"] = token };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(Agora);
        return (new MercadoPagoOrdersPixProvider(http, config, clock.Object, NullLogger<MercadoPagoOrdersPixProvider>.Instance), handler);
    }

    private static PixOrderRequest NovaRequest(Guid? id = null, decimal valor = 50m) =>
        new(id ?? Guid.NewGuid(), valor, "Pedido teste", "cliente@exemplo.com", "Maria");

    [Fact]
    public async Task CriarOrderAsync_EnviaBodyEHeadersDoContrato()
    {
        var (provider, handler) = CriarProvider(HttpStatusCode.Created, SuccessJson);
        var id = Guid.NewGuid();

        await provider.CriarOrderAsync(NovaRequest(id));

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.AbsolutePath.Should().EndWith("v1/orders");
        handler.Request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
        handler.Request.Headers.GetValues("X-Idempotency-Key").Should().ContainSingle().Which.Should().Be(id.ToString());

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
        root.GetProperty("config").GetProperty("payment").GetProperty("expiration_time").GetString().Should().Be("PT30M");
        root.TryGetProperty("expiration_time", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CriarOrderAsync_ValorComCulturaPtBr_UsaPonto()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            var (provider, handler) = CriarProvider(HttpStatusCode.Created, SuccessJson);

            await provider.CriarOrderAsync(NovaRequest(valor: 50.5m));

            using var doc = JsonDocument.Parse(handler.RequestBody!);
            doc.RootElement.GetProperty("total_amount").GetString().Should().Be("50.50");
            doc.RootElement.GetProperty("transactions").GetProperty("payments")[0]
                .GetProperty("amount").GetString().Should().Be("50.50");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task CriarOrderAsync_RespostaDeSucesso_MapeiaOrderIdQrCodeETicket()
    {
        var (provider, _) = CriarProvider(HttpStatusCode.Created, SuccessJson);

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
        { "id": "ORD1", "transactions": { "payments": [ { "id": "PAY1", "payment_method": { "id": "pix", "type": "bank_transfer" } } ] } }
        """;
        var (provider, _) = CriarProvider(HttpStatusCode.Created, semQr);

        var act = () => provider.CriarOrderAsync(NovaRequest());

        await act.Should().ThrowAsync<PixOrdersProviderException>();
    }

    [Fact]
    public async Task CriarOrderAsync_Http400_LancaExcecaoComStatusEMensagemDoErrors()
    {
        const string erro = """
        { "errors": [ { "code": "invalid_email", "message": "Payer email is invalid" } ] }
        """;
        var (provider, _) = CriarProvider(HttpStatusCode.BadRequest, erro);

        var act = () => provider.CriarOrderAsync(NovaRequest());

        var ex = (await act.Should().ThrowAsync<PixOrdersProviderException>()).Which;
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Contain("invalid_email").And.Contain("Payer email is invalid");
    }

    [Fact]
    public async Task ConsultarOrderAsync_Sucesso_RetornaStatusEStatusDetail()
    {
        var (provider, handler) = CriarProvider(HttpStatusCode.OK,
            """{ "id": "ORD1", "status": "processed", "status_detail": "accredited" }""");

        var status = await provider.ConsultarOrderAsync("ORD1");

        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri!.AbsolutePath.Should().EndWith("v1/orders/ORD1");
        status.Should().Be(new PixOrderStatus("processed", "accredited"));
    }

    [Fact]
    public async Task ConsultarOrderAsync_404_RetornaNull()
    {
        var (provider, _) = CriarProvider(HttpStatusCode.NotFound, """{ "errors": [ { "code": "not_found", "message": "x" } ] }""");

        var status = await provider.ConsultarOrderAsync("ORD404");

        status.Should().BeNull();
    }

    [Fact]
    public async Task CriarOrderAsync_SemAccessToken_LancaInvalidOperationException()
    {
        var (provider, _) = CriarProvider(HttpStatusCode.Created, SuccessJson, token: null);

        var act = () => provider.CriarOrderAsync(NovaRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}