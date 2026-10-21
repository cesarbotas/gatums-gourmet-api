using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace GatumsGourmet.Api.Extensions;

/// <summary>
/// Configura o OpenAPI nativo do .NET 10 (Microsoft.AspNetCore.OpenApi) e o Scalar.
/// Declara o esquema de segurança JWT Bearer no documento para que o Scalar consiga
/// aplicar o token informado na UI às requisições (o Scalar segue o contrato OpenAPI,
/// não o middleware de autenticação).
/// </summary>
public static class OpenApiExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddGatumsOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new()
                {
                    Title = "Gatums Gourmet API",
                    Version = "v1",
                    Description = "API do sistema Gatums Gourmet"
                };

                // Declara o esquema de segurança JWT Bearer (Microsoft.OpenApi 2.x).
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??=
                    new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Informe o token JWT. Exemplo: eyJhbGci..."
                };

                // Aplica o esquema globalmente para que o Scalar exiba o botão de autorização.
                document.Security ??= new List<OpenApiSecurityRequirement>();
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerScheme, document)] = []
                });

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapGatumsOpenApi(this WebApplication app)
    {
        // Gera o endpoint JSON do OpenAPI (ex: /openapi/v1.json)
        app.MapOpenApi();

        // Ativa a interface visual do Scalar apontando para o endpoint nativo
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Gatums Gourmet Documentation")
                   .WithTheme(ScalarTheme.DeepSpace)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        return app;
    }
}
