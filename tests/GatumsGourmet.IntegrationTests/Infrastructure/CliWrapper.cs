using System.Diagnostics;

namespace GatumsGourmet.IntegrationTests.Infrastructure;

/// <summary>
/// Wrapper para executar comandos CLI (docker, pg_isready, etc.) via Process.
/// Usado para subir e descer o Docker dos testes de integração.
/// </summary>
public class CliWrapper
{
    /// <summary>
    /// Executa um comando e aguarda a conclusão.
    /// </summary>
    public async Task<CliResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Não foi possível iniciar: {fileName} {arguments}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new CliResult(process.ExitCode, stdout.Trim(), stderr.Trim());
    }
}

/// <summary>
/// Resultado de um comando CLI.
/// </summary>
public record CliResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Success => ExitCode == 0;
    public string Output => string.IsNullOrEmpty(StdErr) ? StdOut : $"{StdOut}{Environment.NewLine}{StdErr}";
}
