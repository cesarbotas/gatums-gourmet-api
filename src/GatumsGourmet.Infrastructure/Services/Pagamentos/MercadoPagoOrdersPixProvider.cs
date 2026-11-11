using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Infrastructure.Services.Pagamentos;

/// <summary>
/// Integração de Pix com a Orders API do Mercado Pago (v1/orders), em paralelo ao provider V1 (v1/payments).
/// O HttpClient é registrado via IHttpClientFactory com BaseAddress configurado para https://api.mercadopago.com/.
/// </summary>
public sealed class MercadoPagoOrdersPixProvider(
    HttpClient http,
    IConfiguration configuration,
    IClock clock,
    ILogger<MercadoPagoOrdersPixProvider> logger) : IPixOrdersProvider
{
    private const int MinExpirationMinutes = 30;
    private const int MaxExpirationMinutes = 43200; // 30 dias
    private const int DefaultExpirationMinutes = 30;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string GetAccessToken()
    {
        var token = configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("MercadoPago:AccessToken não está configurado. Configure a variável de ambiente MercadoPago__AccessToken.");
        }

        return token;
    }

    private int GetExpirationMinutes()
    {
        var configured = configuration.GetValue<int?>("MercadoPago:PixExpirationMinutes") ?? DefaultExpirationMinutes;
        return Math.Clamp(configured, MinExpirationMinutes, MaxExpirationMinutes);
    }

    public async Task<PixOrderResult> CriarOrderAsync(PixOrderRequest request, CancellationToken ct = default)
    {
        var token = GetAccessToken();
        var expirationMinutes = GetExpirationMinutes();
        var amount = request.Valor.ToString("F2", CultureInfo.InvariantCulture);
        var transacaoId = request.TransacaoId.ToString();

        var body = new CreateOrderBody
        {
            Type = "online",
            ProcessingMode = "automatic",
            ExternalReference = transacaoId,
            TotalAmount = amount,
            Description = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao,
            Transactions = new OrderTransactions
            {
                Payments =
                [
                    new OrderPaymentInput
                    {
                        Amount = amount,
                        PaymentMethod = new OrderPaymentMethodInput { Id = "pix", Type = "bank_transfer" }
                    }
                ]
            },
            Payer = new OrderPayer
            {
                Email = request.PayerEmail,
                FirstName = string.IsNullOrWhiteSpace(request.PayerNome) ? null : request.PayerNome
            },
            Config = new OrderConfig
            {
                Payment = new OrderConfigPayment { ExpirationTime = $"PT{expirationMinutes}M" }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/orders")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Add("X-Idempotency-Key", transacaoId);

        logger.LogInformation("Criando order Pix no Mercado Pago. TransacaoId={TransacaoId} Valor={Valor}", request.TransacaoId, amount);

        using var response = await http.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Erro retornado pela Orders API do Mercado Pago ({StatusCode}). TransacaoId={TransacaoId}", (int)response.StatusCode, request.TransacaoId);
            throw new PixOrdersProviderException(
                $"Falha na Orders API do Mercado Pago ({(int)response.StatusCode}): {FormatErrors(errorBody)}",
                (int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions, ct);
        var paymentMethod = order?.Transactions?.Payments?.FirstOrDefault()?.PaymentMethod;

        if (order is null || string.IsNullOrWhiteSpace(order.Id) || string.IsNullOrWhiteSpace(paymentMethod?.QrCode))
        {
            logger.LogError("Resposta da Orders API sem order id ou qr_code. TransacaoId={TransacaoId}", request.TransacaoId);
            throw new PixOrdersProviderException(
                "Resposta da Orders API do Mercado Pago inválida: qr_code ausente.",
                (int)response.StatusCode);
        }

        logger.LogInformation("Order Pix criada no Mercado Pago. TransacaoId={TransacaoId} OrderId={OrderId}", request.TransacaoId, order.Id);

        return new PixOrderResult(
            OrderId: order.Id,
            CopiaCola: paymentMethod.QrCode,
            QrCodeBase64: paymentMethod.QrCodeBase64,
            TicketUrl: paymentMethod.TicketUrl,
            ExpiraEm: clock.UtcNow.AddMinutes(expirationMinutes));
    }

    public async Task<PixOrderStatus?> ConsultarOrderAsync(string orderId, CancellationToken ct = default)
    {
        var token = GetAccessToken();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/orders/{Uri.EscapeDataString(orderId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(httpRequest, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("Order {OrderId} não encontrada no Mercado Pago.", orderId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Erro ao consultar order {OrderId} no Mercado Pago ({StatusCode}).", orderId, (int)response.StatusCode);
            throw new PixOrdersProviderException(
                $"Falha ao consultar order no Mercado Pago ({(int)response.StatusCode}): {FormatErrors(errorBody)}",
                (int)response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions, ct);
        return order is null || string.IsNullOrWhiteSpace(order.Status)
            ? null
            : new PixOrderStatus(order.Status, order.StatusDetail);
    }

    private static string FormatErrors(string errorBody)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<ErrorResponse>(errorBody, JsonOptions);
            if (parsed?.Errors is { Count: > 0 })
            {
                var sb = new StringBuilder();
                foreach (var e in parsed.Errors)
                {
                    if (sb.Length > 0) sb.Append("; ");
                    sb.Append(e.Code).Append(": ").Append(e.Message);
                }

                return sb.ToString();
            }
        }
        catch (JsonException)
        {
            // corpo não é JSON — cai no texto bruto
        }

        return string.IsNullOrWhiteSpace(errorBody) ? "sem corpo na resposta" : errorBody;
    }

    #region DTOs internos Mercado Pago (Orders)

    private sealed record CreateOrderBody
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = default!;

        [JsonPropertyName("processing_mode")]
        public string ProcessingMode { get; init; } = default!;

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; init; } = default!;

        [JsonPropertyName("total_amount")]
        public string TotalAmount { get; init; } = default!;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("transactions")]
        public OrderTransactions Transactions { get; init; } = default!;

        [JsonPropertyName("payer")]
        public OrderPayer Payer { get; init; } = default!;

        [JsonPropertyName("config")]
        public OrderConfig Config { get; init; } = default!;
    }

    private sealed record OrderTransactions
    {
        [JsonPropertyName("payments")]
        public List<OrderPaymentInput> Payments { get; init; } = [];
    }

    private sealed record OrderPaymentInput
    {
        [JsonPropertyName("amount")]
        public string Amount { get; init; } = default!;

        [JsonPropertyName("payment_method")]
        public OrderPaymentMethodInput PaymentMethod { get; init; } = default!;
    }

    private sealed record OrderPaymentMethodInput
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = default!;

        [JsonPropertyName("type")]
        public string Type { get; init; } = default!;
    }

    private sealed record OrderPayer
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = default!;

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }
    }

    private sealed record OrderConfig
    {
        [JsonPropertyName("payment")]
        public OrderConfigPayment Payment { get; init; } = default!;
    }

    private sealed record OrderConfigPayment
    {
        [JsonPropertyName("expiration_time")]
        public string ExpirationTime { get; init; } = default!;
    }

    private sealed record OrderResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; init; }

        [JsonPropertyName("transactions")]
        public OrderResponseTransactions? Transactions { get; init; }
    }

    private sealed record OrderResponseTransactions
    {
        [JsonPropertyName("payments")]
        public List<OrderResponsePayment>? Payments { get; init; }
    }

    private sealed record OrderResponsePayment
    {
        [JsonPropertyName("payment_method")]
        public OrderResponsePaymentMethod? PaymentMethod { get; init; }
    }

    private sealed record OrderResponsePaymentMethod
    {
        [JsonPropertyName("qr_code")]
        public string? QrCode { get; init; }

        [JsonPropertyName("qr_code_base64")]
        public string? QrCodeBase64 { get; init; }

        [JsonPropertyName("ticket_url")]
        public string? TicketUrl { get; init; }
    }

    private sealed record ErrorResponse
    {
        [JsonPropertyName("errors")]
        public List<ErrorItem>? Errors { get; init; }
    }

    private sealed record ErrorItem
    {
        [JsonPropertyName("code")]
        public string? Code { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    #endregion
}