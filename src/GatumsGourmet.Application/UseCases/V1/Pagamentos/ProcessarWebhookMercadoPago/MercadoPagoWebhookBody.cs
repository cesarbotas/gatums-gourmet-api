using System.Text.Json.Serialization;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarWebhookMercadoPago;

public record MercadoPagoWebhookBody
{
    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("data")]
    public MercadoPagoWebhookData? Data { get; init; }
};

public record MercadoPagoWebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}
