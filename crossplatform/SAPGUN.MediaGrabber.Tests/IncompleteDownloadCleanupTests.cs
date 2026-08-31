using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class IncompleteDownloadCleanupTests
{
    [Theory]
    [InlineData("clip.mp4.part", true)]
    [InlineData("clip.mp4.ytdl", true)]
    [InlineData("clip.mp4.part-Frag1", true)]
    [InlineData("song_X.mp4", true)]
    [InlineData("finished.mp4", false)]
    [InlineData("notes.txt", false)]
    public void ClassifiesLeftoverNames(string name, bool leftover)
    {
        Assert.Equal(leftover, IncompleteDownloadCleanup.IsLeftoverName(name));
    }

    [Fact]
    public void DeletesOnlyNewLeftovers()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sapgun-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var oldPart = Path.Combine(dir, "old.mp4.part");
            var newPart = Path.Combine(dir, "new.mp4.part");
            var keep = Path.Combine(dir, "keep.mp4");
            File.WriteAllText(oldPart, "old");
            File.SetLastWriteTimeUtc(oldPart, DateTime.UtcNow.AddHours(-2));
            File.WriteAllText(newPart, "new");
            File.WriteAllText(keep, "keep");

            var deleted = IncompleteDownloadCleanup.DeleteLeftovers(dir, DateTimeOffset.UtcNow.AddMinutes(-1));
            Assert.Equal(1, deleted);
            Assert.True(File.Exists(oldPart));
            Assert.False(File.Exists(newPart));
            Assert.True(File.Exists(keep));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
