namespace GatumsGourmet.Api.Extensions;

/// <summary>
/// Log de diagnóstico do storage de imagens no startup. Mostra, no log do host
/// (ex.: Render), se a seção <c>Storage</c> chegou ao processo e quais campos
/// estão preenchidos — SEM expor segredos (AccessKey/SecretKey saem apenas como
/// bool "preenchido"). Útil para diagnosticar "ServiceUrl vazio" quando a
/// variável de ambiente Storage__ServiceUrl não é injetada no ambiente certo.
/// </summary>
public static class StorageDiagnosticsExtensions
{
    public static WebApplication LogStorageDiagnostics(this WebApplication app)
    {
        var cfg = app.Configuration;
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GatumsGourmet.Startup.Storage");

        var serviceUrl = cfg["Storage:ServiceUrl"];
        var publicBaseUrl = cfg["Storage:PublicBaseUrl"];
        var accessKey = cfg["Storage:AccessKey"];
        var secretKey = cfg["Storage:SecretKey"];
        var bucket = cfg["Storage:Bucket"];
        var region = cfg["Storage:Region"];
        var forcePathStyle = cfg["Storage:ForcePathStyle"];
        var orphanCleanup = cfg["Storage:OrphanCleanupEnabled"];

        var configurado = !string.IsNullOrWhiteSpace(serviceUrl);

        logger.LogInformation(
            "[Storage] Diagnóstico no startup: ambiente={Ambiente} | configurado={Configurado} | " +
            "ServiceUrlPreenchida={ServiceUrlOk} | Bucket='{Bucket}' | Region='{Region}' | " +
            "PublicBaseUrlPreenchida={PublicBaseOk} | AccessKeyPreenchida={AccessKeyOk} | " +
            "SecretKeyPreenchida={SecretKeyOk} | ForcePathStyle='{ForcePathStyle}' | " +
            "OrphanCleanupEnabled='{OrphanCleanup}' | ImagemStorage={Implementacao}",
            app.Environment.EnvironmentName,
            configurado,
            !string.IsNullOrWhiteSpace(serviceUrl),
            bucket ?? "(null)",
            region ?? "(null)",
            !string.IsNullOrWhiteSpace(publicBaseUrl),
            !string.IsNullOrWhiteSpace(accessKey),
            !string.IsNullOrWhiteSpace(secretKey),
            forcePathStyle ?? "(null)",
            orphanCleanup ?? "(null)",
            configurado ? "S3ImageStorage" : "UnconfiguredImageStorage");

        if (!configurado)
        {
            logger.LogWarning(
                "[Storage] ServiceUrl está vazio no ambiente '{Ambiente}'. O upload de imagens " +
                "responderá 400 (indisponível). Verifique a variável de ambiente " +
                "Storage__ServiceUrl (duplo underscore) no serviço correto do host.",
                app.Environment.EnvironmentName);
        }

        return app;
    }
}
