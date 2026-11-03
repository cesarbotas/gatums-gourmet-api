using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Api.Extensions;

/// <summary>
/// Utility para logar exceções com toda a cadeia de inner exceptions recursivamente.
/// Percorre <see cref="Exception.InnerException"/> (cadeia linear) e
/// <see cref="AggregateException.InnerExceptions"/> (exceções paralelas),
/// evitando duplicação quando ambos coexistem.
/// </summary>
public static class DeepExceptionLogger
{
    /// <summary>
    /// Loga a exceção principal (com stack trace via ILogger) e em seguida
    /// percorre e loga cada inner exception individualmente, indicando o nível
    /// de aninhamento, o tipo e a mensagem.
    /// </summary>
    public static void LogDeep(this ILogger logger, Exception ex, string message, params object[] args)
    {
        // Log principal — ILogger inclui o stack trace automaticamente.
        logger.LogError(ex, message, args);

        // Percorre a cadeia de inner exceptions.
        LogInnerChain(logger, ex, depth: 0);
    }

    /// <summary>
    /// Loga a exceção em nível Warning (para erros esperados/degradáveis)
    /// mantendo o deep logging de inner exceptions.
    /// </summary>
    public static void LogDeepWarning(this ILogger logger, Exception ex, string message, params object[] args)
    {
        logger.LogWarning(ex, message, args);
        LogInnerChain(logger, ex, depth: 0, warningLevel: true);
    }

    private static void LogInnerChain(ILogger logger, Exception ex, int depth, bool warningLevel = false)
    {
        // ── InnerException (cadeia linear: ex → ex.InnerException → ...) ──
        if (ex.InnerException is not null)
        {
            LogOne(logger, ex.InnerException, depth + 1, warningLevel);
            LogInnerChain(logger, ex.InnerException, depth + 1, warningLevel);
        }

        // ── AggregateException (exceções paralelas: Task.WhenAll, etc.) ──
        if (ex is AggregateException ae)
        {
            for (var i = 0; i < ae.InnerExceptions.Count; i++)
            {
                // Não duplica se já foi coberto pelo InnerException acima.
                if (ae.InnerExceptions[i] == ex.InnerException)
                    continue;

                LogOne(logger, ae.InnerExceptions[i], depth + 1, warningLevel, $"Aggregate[{i}]");
                LogInnerChain(logger, ae.InnerExceptions[i], depth + 1, warningLevel);
            }
        }

        // ── ReflectionTypeLoadException (pode ter LoaderExceptions) ──
        if (ex is System.Reflection.ReflectionTypeLoadException rtle)
        {
            for (var i = 0; i < rtle.LoaderExceptions.Length; i++)
            {
                var le = rtle.LoaderExceptions[i];
                if (le is null) continue;
                LogOne(logger, le, depth + 1, warningLevel, $"Loader[{i}]");
            }
        }
    }

    private static void LogOne(ILogger logger, Exception ex, int depth, bool warningLevel, string? label = null)
    {
        var prefix = label ?? $"Inner[{depth}]";
        var indent = new string(' ', (depth - 1) * 2);

        if (warningLevel)
        {
            logger.LogWarning("{Indent}└─ {Prefix}: {ExceptionType}: {Message}",
                indent, prefix, ex.GetType().FullName, ex.Message);
        }
        else
        {
            logger.LogError("{Indent}└─ {Prefix}: {ExceptionType}: {Message}",
                indent, prefix, ex.GetType().FullName, ex.Message);
        }

        // Loga o stack trace apenas no primeiro nível de inner (evita spam).
        if (depth == 1 && ex.StackTrace is not null)
        {
            if (warningLevel)
                logger.LogWarning("{Indent}   StackTrace: {StackTrace}", indent, ex.StackTrace);
            else
                logger.LogError("{Indent}   StackTrace: {StackTrace}", indent, ex.StackTrace);
        }
    }
}
