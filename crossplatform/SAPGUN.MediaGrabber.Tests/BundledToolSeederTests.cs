using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class BundledToolSeederTests
{
    [Fact]
    public void SeedsMissingTools()
    {
        var root = Path.Combine(Path.GetTempPath(), "sapgun-seed-" + Guid.NewGuid().ToString("N"));
        var seed = Path.Combine(root, "seed");
        var tools = Path.Combine(root, "tools");
        Directory.CreateDirectory(seed);
        File.WriteAllText(Path.Combine(seed, "ffmpeg"), "ff");
        File.WriteAllText(Path.Combine(seed, "yt-dlp"), "yt");
        try
        {
            Assert.Equal(2, BundledToolSeeder.Seed(seed, tools));
            Assert.True(File.Exists(Path.Combine(tools, "ffmpeg")));
            Assert.True(File.Exists(Path.Combine(tools, "yt-dlp")));
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }

    [Fact]
    public void PreservesExistingYtDlpButRefreshesFfmpeg()
    {
        var root = Path.Combine(Path.GetTempPath(), "sapgun-seed-" + Guid.NewGuid().ToString("N"));
        var seed = Path.Combine(root, "seed");
        var tools = Path.Combine(root, "tools");
        Directory.CreateDirectory(seed);
        Directory.CreateDirectory(tools);
        File.WriteAllText(Path.Combine(seed, "yt-dlp"), "new-yt");
        File.WriteAllText(Path.Combine(seed, "ffmpeg"), "new-ff");
        File.WriteAllText(Path.Combine(tools, "yt-dlp"), "old-yt");
        File.WriteAllText(Path.Combine(tools, "ffmpeg"), "old-ff");
        File.SetLastWriteTimeUtc(Path.Combine(tools, "ffmpeg"), DateTime.UtcNow.AddDays(-2));
        File.SetLastWriteTimeUtc(Path.Combine(seed, "ffmpeg"), DateTime.UtcNow);
        try
        {
            BundledToolSeeder.Seed(seed, tools);
            Assert.Equal("old-yt", File.ReadAllText(Path.Combine(tools, "yt-dlp")));
            Assert.Equal("new-ff", File.ReadAllText(Path.Combine(tools, "ffmpeg")));
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }
}
