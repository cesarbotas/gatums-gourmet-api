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
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation"),
                DomainException => (StatusCodes.Status400BadRequest, "Domain Error"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails { Title = title, Detail = ex.Message, Status = status };
            await ctx.Response.WriteAsJsonAsync(problem);
        }
    }
}
