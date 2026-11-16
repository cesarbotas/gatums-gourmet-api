using System.Net;
using System.Text;
using GatumsGourmet.Infrastructure.Services.Pagamentos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.UnitTests.Infrastructure.Services.Pagamentos;

/// <summary>
/// Fixture compartilhada pelos testes de <see cref="MercadoPagoOrdersPixProvider"/>.
/// Encapsula o FakeHandler HTTP, o JSON de sucesso padrão e a criação de provider configurado.
/// </summary>
public sealed class MercadoPagoOrdersPixProviderFixture
{
    public static readonly DateTime Agora = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    public string SuccessJson => """
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

    /// <summary>Handler HTTP falso que captura a requisição e retorna a resposta configurada.</summary>
    public sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    public (MercadoPagoOrdersPixProvider Provider, FakeHandler Handler) CriarProvider(
        HttpStatusCode status,
        string body,
        string? token = "test-token")
    {
        var handler = new FakeHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mercadopago.test/") };

        var values = new Dictionary<string, string?> { ["MercadoPago:AccessToken"] = token };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(Agora);

        return (new MercadoPagoOrdersPixProvider(
            http, config, clock.Object,
            NullLogger<MercadoPagoOrdersPixProvider>.Instance), handler);
    }
}
