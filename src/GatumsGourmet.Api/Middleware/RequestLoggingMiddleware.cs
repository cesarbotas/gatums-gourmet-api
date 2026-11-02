using System.Diagnostics;
using System.Security.Claims;

namespace GatumsGourmet.Api.Middleware;

/// <summary>
/// Middleware que loga o fluxo de cada requisição HTTP: método, path,
/// status code, userId (do JWT se autenticado) e duração em ms.
/// Captura a exceção para log de erro no finally, garantindo que todo
/// fluxo — sucesso ou falha — deixe rastro no log.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();
        var method = ctx.Request.Method;
        var path = ctx.Request.Path.Value ?? "/";
        var userId = ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? ctx.User?.FindFirst("sub")?.Value
                     ?? "anonimo";
        var requestId = ctx.TraceIdentifier;

        logger.LogInformation("→ {Method} {Path} | RequestId={RequestId} | User={UserId}",
            method, path, requestId, userId);

        try
        {
            await next(ctx);
        }
        finally
        {
            sw.Stop();
            var status = ctx.Response.StatusCode;
            var level = status >= 500 ? LogLevel.Error
                      : status >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            logger.Log(level,
                "← {Method} {Path} → {Status} ({ElapsedMs}ms) | RequestId={RequestId} | User={UserId}",
                method, path, status, sw.ElapsedMilliseconds, requestId, userId);
        }
    }
}
