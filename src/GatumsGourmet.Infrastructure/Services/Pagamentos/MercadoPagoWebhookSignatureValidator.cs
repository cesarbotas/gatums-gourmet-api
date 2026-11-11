using System.Security.Cryptography;
using System.Text;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Infrastructure.Services.Pagamentos;

/// <summary>
/// Valida o header <c>x-signature</c> (<c>ts=&lt;ms unix&gt;,v1=&lt;hex&gt;</c>) dos webhooks do Mercado Pago:
/// HMAC-SHA256 hex minúsculo de <c>id:{data.id};request-id:{x-request-id};ts:{ts};</c>.
/// </summary>
public class MercadoPagoWebhookSignatureValidator : IMercadoPagoWebhookSignatureValidator
{
    private static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly IClock _clock;
    private readonly ILogger<MercadoPagoWebhookSignatureValidator> _logger;

    public MercadoPagoWebhookSignatureValidator(
        IConfiguration configuration,
        IHostEnvironment environment,
        IClock clock,
        ILogger<MercadoPagoWebhookSignatureValidator> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _clock = clock;
        _logger = logger;
    }

    public bool IsValid(string? signatureHeader, string? requestId, string? dataId)
    {
        var segredo = _configuration["MercadoPago:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(segredo))
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("MercadoPago:WebhookSecret ausente; assinatura do webhook não validada (Development).");
                return true;
            }

            _logger.LogError("MercadoPago:WebhookSecret ausente; webhook rejeitado.");
            return false;
        }

        if (!TryParse(signatureHeader, out var ts, out var v1))
        {
            _logger.LogWarning("Header x-signature ausente ou malformado.");
            return false;
        }

        if (!long.TryParse(ts, out var tsMs))
        {
            _logger.LogWarning("x-signature com ts inválido.");
            return false;
        }

        var agoraMs = new DateTimeOffset(DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        if (Math.Abs(agoraMs - tsMs) > Tolerancia.TotalMilliseconds)
        {
            _logger.LogWarning("x-signature com ts fora da tolerância de {Minutos} min.", Tolerancia.TotalMinutes);
            return false;
        }

        var manifesto = $"id:{(dataId ?? string.Empty).ToLowerInvariant()};request-id:{requestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(segredo));
        var esperado = Encoding.ASCII.GetBytes(
            Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifesto))).ToLowerInvariant());
        var recebido = Encoding.ASCII.GetBytes(v1.ToLowerInvariant());

        var valido = CryptographicOperations.FixedTimeEquals(esperado, recebido);
        if (!valido) _logger.LogWarning("x-signature não confere com o HMAC esperado.");
        return valido;
    }

    private static bool TryParse(string? header, out string ts, out string v1)
    {
        ts = string.Empty;
        v1 = string.Empty;
        if (string.IsNullOrWhiteSpace(header)) return false;

        foreach (var parte in header.Split(','))
        {
            var kv = parte.Split('=', 2);
            if (kv.Length != 2) continue;
            var chave = kv[0].Trim();
            var valor = kv[1].Trim();
            if (chave == "ts") ts = valor;
            else if (chave == "v1") v1 = valor;
        }

        return ts.Length > 0 && v1.Length > 0;
    }
}
