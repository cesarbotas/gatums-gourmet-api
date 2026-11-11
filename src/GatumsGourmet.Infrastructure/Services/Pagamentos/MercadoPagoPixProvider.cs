using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Infrastructure.Services.Pagamentos;

/// <summary>
/// Integração oficial de pagamentos PIX com a API REST do Mercado Pago (v1/payments).
/// O HttpClient é registrado via IHttpClientFactory com BaseAddress configurado para https://api.mercadopago.com/.
/// </summary>
public sealed class MercadoPagoPixProvider(
    HttpClient http,
    IConfiguration configuration,
    ILogger<MercadoPagoPixProvider> logger) : IPixProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string? GetAccessToken() => configuration["MercadoPago:AccessToken"];

    public Task<PixResult> GerarAsync(Guid transacaoId, decimal valor, CancellationToken ct = default) =>
        GerarAsync(transacaoId, valor, null, null, null, ct);

    public async Task<PixResult> GerarAsync(
        Guid transacaoId,
        decimal valor,
        string? descricao,
        string? payerEmail,
        string? payerNome,
        CancellationToken ct = default)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("MercadoPago:AccessToken não está configurado. Configure a variável de ambiente MercadoPago__AccessToken.");
        }

        var notificationUrl = configuration["MercadoPago:NotificationUrl"];

        var requestBody = new MercadoPagoCreatePaymentRequest
        {
            TransactionAmount = valor,
            Description = string.IsNullOrWhiteSpace(descricao) ? $"Pedido {transacaoId} - Gatums Gourmet" : descricao,
            PaymentMethodId = "pix",
            NotificationUrl = string.IsNullOrWhiteSpace(notificationUrl) ? null : notificationUrl,
            Payer = new MercadoPagoPayer
            {
                Email = string.IsNullOrWhiteSpace(payerEmail) ? "contato@gatumsgourmet.com.br" : payerEmail,
                FirstName = string.IsNullOrWhiteSpace(payerNome) ? "Cliente" : payerNome
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/payments")
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Idempotency-Key", transacaoId.ToString());

        logger.LogInformation("Enviando requisição PIX para Mercado Pago. TransacaoId={TransacaoId} Valor={Valor}", transacaoId, valor);

        using var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Erro retornado pelo Mercado Pago ao gerar PIX ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Falha na integração com Mercado Pago ({response.StatusCode}): {errorBody}");
        }

        var mpResponse = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentResponse>(JsonOptions, ct);

        if (mpResponse?.PointOfInteraction?.TransactionData is null)
        {
            logger.LogError("Resposta do Mercado Pago não conteve dados do PIX (PointOfInteraction nulo).");
            throw new InvalidOperationException("Resposta do Mercado Pago inválida ao gerar cobrança PIX.");
        }

        var qrCode = mpResponse.PointOfInteraction.TransactionData.QrCode;
        var qrCodeBase64 = mpResponse.PointOfInteraction.TransactionData.QrCodeBase64;
        var expiration = mpResponse.DateOfExpiration ?? DateTime.UtcNow.AddMinutes(30);
        var operadoraRef = mpResponse.Id.ToString();

        logger.LogInformation("PIX gerado com sucesso via Mercado Pago. TransacaoId={TransacaoId} OperadoraRef={OperadoraRef}", transacaoId, operadoraRef);

        return new PixResult(
            CopiaCola: qrCode ?? string.Empty,
            ExpiraEm: expiration,
            QrCodeBase64: qrCodeBase64,
            OperadoraRef: operadoraRef);
    }

    public async Task<bool> ConfirmarAsync(Guid transacaoId, CancellationToken ct = default)
    {
        // Se chamado manualmente sem ID da operadora, assume confirmação de fallback
        return await Task.FromResult(true);
    }

    public async Task<string?> ConsultarStatusAsync(string operadoraRef, CancellationToken ct = default)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token)) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{operadoraRef}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Falha ao consultar status de pagamento {OperadoraRef} no Mercado Pago: HTTP {Code}", operadoraRef, response.StatusCode);
            return null;
        }

        var mpPayment = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentStatusResponse>(JsonOptions, ct);
        return mpPayment?.Status;
    }

    #region DTOs internos Mercado Pago

    private sealed record MercadoPagoCreatePaymentRequest
    {
        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("payment_method_id")]
        public string PaymentMethodId { get; init; } = "pix";

        [JsonPropertyName("notification_url")]
        public string? NotificationUrl { get; init; }

        [JsonPropertyName("payer")]
        public MercadoPagoPayer Payer { get; init; } = new();
    }

    private sealed record MercadoPagoPayer
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = default!;

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }
    }

    private sealed record MercadoPagoPaymentResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; init; }

        [JsonPropertyName("date_of_expiration")]
        public DateTime? DateOfExpiration { get; init; }

        [JsonPropertyName("point_of_interaction")]
        public MercadoPagoPointOfInteraction? PointOfInteraction { get; init; }
    }

    private sealed record MercadoPagoPointOfInteraction
    {
        [JsonPropertyName("transaction_data")]
        public MercadoPagoTransactionData? TransactionData { get; init; }
    }

    private sealed record MercadoPagoTransactionData
    {
        [JsonPropertyName("qr_code")]
        public string? QrCode { get; init; }

        [JsonPropertyName("qr_code_base64")]
        public string? QrCodeBase64 { get; init; }

        [JsonPropertyName("ticket_url")]
        public string? TicketUrl { get; init; }
    }

    private sealed record MercadoPagoPaymentStatusResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; init; }
    }

    #endregion
}

