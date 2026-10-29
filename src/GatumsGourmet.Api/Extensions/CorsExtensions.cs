using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GatumsGourmet.Api.Extensions;

/// <summary>
/// Configura o CORS por ambiente:
/// <list type="bullet">
/// <item>Development/LocalDocker: libera qualquer origem (conveniência local).</item>
/// <item>Demais ambientes (Release/Production): restringe às origens exatas
/// (<c>Cors:AllowedOrigins</c>) e aos padrões curinga
/// (<c>Cors:AllowedOriginPatterns</c>, ex.: <c>https://gatums-gourmet*.vercel.app</c>).</item>
/// </list>
/// Como a autenticação é Bearer (não cookie), NÃO habilitamos AllowCredentials.
/// A policy valida a configuração no startup (fail-fast) para não subir HML/PRD
/// com CORS quebrado silenciosamente.
///
/// A policy é montada por um <see cref="IConfigureOptions{TOptions}"/> resolvido do
/// DI: assim a <see cref="IConfiguration"/> lida é a final (todas as fontes já
/// mescladas), e não uma captura do momento do registro — o que mantém o
/// comportamento correto em testes que injetam config via ConfigureAppConfiguration.
/// </summary>
public static class CorsExtensions
{
    public const string PolicyName = "GatumsCors";

    public static IServiceCollection AddGatumsCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddSingleton<IConfigureOptions<CorsOptions>, GatumsCorsOptionsSetup>();
        return services;
    }

    public static IApplicationBuilder UseGatumsCors(this IApplicationBuilder app)
    {
        // Força a resolução das CorsOptions AGORA (no setup do pipeline, durante o
        // startup) para que a validação fail-fast rode antes de servir requisições
        // — em vez de só na primeira avaliação de CORS. Se a config de Release/PRD
        // estiver vazia/ inválida, o host não sobe.
        _ = app.ApplicationServices.GetRequiredService<IOptions<CorsOptions>>().Value;
        return app.UseCors(PolicyName);
    }

    /// <summary>Remove barra final e espaços; descarta entradas vazias.</summary>
    internal static string[] NormalizeOrigins(IEnumerable<string> origins) =>
        origins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .ToArray();

    /// <summary>
    /// Converte um padrão com <c>*</c> (ex.: <c>https://gatums-gourmet*.vercel.app</c>)
    /// numa regex âncorada, onde <c>*</c> casa qualquer sequência exceto a barra de
    /// path (para o curinga não atravessar o host). O resto é escapado literalmente.
    /// </summary>
    internal static Regex PatternToRegex(string pattern)
    {
        var normalizado = pattern.Trim().TrimEnd('/');
        var escapado = Regex.Escape(normalizado).Replace("\\*", "[^/]*");
        return new Regex($"^{escapado}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Fail-fast: em Release/Production, exige ao menos uma origem ou padrão, e
    /// valida o formato de cada origem exata (esquema http/https, sem path/barra).
    /// Em outros ambientes (ex.: Test) não falha com lista vazia — nega tudo.
    /// </summary>
    internal static void ValidarConfiguracao(string[] origins, string[] patterns, IHostEnvironment environment)
    {
        var isProdLike = environment.IsProduction() || environment.IsEnvironment("Release");

        if (isProdLike && origins.Length == 0 && patterns.Length == 0)
            throw new InvalidOperationException(
                $"CORS: ambiente '{environment.EnvironmentName}' não tem nenhuma origem configurada. " +
                "Defina Cors:AllowedOrigins e/ou Cors:AllowedOriginPatterns (appsettings ou variável de ambiente).");

        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || uri.AbsolutePath != "/"
                || !string.IsNullOrEmpty(uri.Query))
            {
                throw new InvalidOperationException(
                    $"CORS: origem inválida '{origin}'. Use apenas esquema + host (+ porta), " +
                    "sem path, query nem barra final. Ex.: https://app.gatumsgourmet.com.br");
            }
        }
    }
}

/// <summary>
/// Monta a policy <c>GatumsCors</c> lendo a configuração final (via DI). Ver a
/// documentação de <see cref="CorsExtensions"/> para o comportamento por ambiente.
/// </summary>
internal sealed class GatumsCorsOptionsSetup(
    IConfiguration configuration,
    IHostEnvironment environment) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("LocalDocker"))
        {
            options.AddPolicy(CorsExtensions.PolicyName, p =>
                p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            return;
        }

        var origins = CorsExtensions.NormalizeOrigins(
            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []);
        var patterns = (configuration.GetSection("Cors:AllowedOriginPatterns").Get<string[]>() ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .ToArray();

        CorsExtensions.ValidarConfiguracao(origins, patterns, environment);

        var regexes = patterns.Select(CorsExtensions.PatternToRegex).ToArray();

        options.AddPolicy(CorsExtensions.PolicyName, p =>
        {
            // SetIsOriginAllowed cobre origens exatas E padrões curinga numa única
            // avaliação (não combinamos com WithOrigins para evitar ambiguidade).
            p.SetIsOriginAllowed(origin =>
            {
                var normalizada = origin.TrimEnd('/');
                return origins.Contains(normalizada, StringComparer.OrdinalIgnoreCase)
                    || regexes.Any(rx => rx.IsMatch(normalizada));
            });

            p.AllowAnyHeader().AllowAnyMethod();
        });
    }
}
