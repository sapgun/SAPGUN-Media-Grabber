using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class ProcessRunnerTests
{
    [Fact]
    public async Task ReturnsZeroForTrue()
    {
        if (OperatingSystem.IsWindows()) return;
        var rc = await ProcessRunner.RunAsync("true", Array.Empty<string>(), (_, _) => { });
        Assert.Equal(0, rc);
    }

    [Fact]
    public async Task CancelKillsSleep()
    {
        if (OperatingSystem.IsWindows()) return;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ProcessRunner.RunAsync("sleep", new[] { "20" }, (_, _) => { }, cts.Token));
        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(8), "Process was not cancelled promptly");
    }
}
