using System.Diagnostics;

namespace SapgunMediaGrabber.Updates;

public static class ProcessRunner
{
    public static async Task<int> RunAsync(
        string exe,
        IEnumerable<string> args,
        Action<string, bool> onLine,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        if (!process.Start()) throw new InvalidOperationException("Could not start " + exe);

        using var kill = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch { }
        });

        var stdout = ReadLinesAsync(process.StandardOutput, false, onLine, cancellationToken);
        var stderr = ReadLinesAsync(process.StandardError, true, onLine, cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false); } catch { }
            try { await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); } catch { }
            throw;
        }
    }

    static async Task ReadLinesAsync(StreamReader reader, bool isErr, Action<string, bool> onLine, CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                onLine(line, isErr);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }
}
