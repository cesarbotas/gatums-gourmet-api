using System.Security.Claims;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // Request cancelada pelo cliente — não é erro, não loga nem retorna ProblemDetails.
            logger.LogInformation("Request cancelada pelo cliente: {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);
        }
        catch (Exception ex)
        {
            // Contexto da requisição para o log.
            var method = ctx.Request.Method;
            var path = ctx.Request.Path.Value ?? "/";
            var userId = ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? ctx.User?.FindFirst("sub")?.Value
                         ?? "anonimo";
            var requestId = ctx.TraceIdentifier;

            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation"),
                DomainException => (StatusCodes.Status400BadRequest, "Domain Error"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            // Deep exception logging: loga a exceção principal + toda a cadeia de
            // inner exceptions recursivamente (InnerException + AggregateException).
            logger.LogDeep(ex,
                "Unhandled exception | {Method} {Path} | Status={Status} | RequestId={RequestId} | User={UserId} | ExceptionType={ExceptionType}",
                method, path, status, requestId, userId, ex.GetType().Name);

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/problem+json";

            // Em 500, não expõe a mensagem interna ao cliente (information leak).
            // Em erros de domínio (400/404/422), a mensagem é safe (PT-BR, business-level).
            var detail = status >= 500
                ? "Ocorreu um erro interno. Consulte o log com o RequestId para detalhes."
                : ex.Message;

            var problem = new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = status,
                Instance = path
            };
            problem.Extensions["traceId"] = requestId;

            await ctx.Response.WriteAsJsonAsync(problem);
        }
    }
}
