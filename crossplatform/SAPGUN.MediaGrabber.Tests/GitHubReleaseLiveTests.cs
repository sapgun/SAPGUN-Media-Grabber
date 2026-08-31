using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class GitHubReleaseLiveTests
{
    [Fact]
    public async Task PublicReleasesSelectExpectedAlphaLinuxAsset()
    {
        using var http = new HttpClient();
        var client = new GitHubReleaseClient(http);
        var releases = await client.ListReleasesAsync();

        Assert.Contains(releases, r => r.TagName == "v0.2.2" && !r.Prerelease);
        var alpha = Assert.Single(releases, r => r.TagName == "v0.3.0-alpha.1");
        Assert.True(alpha.Prerelease);

        var linux = UpdateAssetSelector.Select(alpha, PlatformDetector.LinuxX64);
        Assert.NotNull(linux);
        Assert.Equal("SAPGUN-Media-Grabber-v0.3.0-alpha.1-linux-x64.tar.gz", linux!.Package.Name);
        Assert.Equal("SHA256SUMS-linux-x64.txt", linux.ChecksumFile?.Name);
        Assert.True(GitHubReleaseClient.IsAllowedDownloadUrl(linux.Package.BrowserDownloadUrl));

        var macos = UpdateAssetSelector.Select(alpha, PlatformDetector.OsxArm64);
        Assert.NotNull(macos);
        Assert.Equal("SAPGUN-Media-Grabber-v0.3.0-alpha.1-macos-arm64.tar.gz", macos!.Package.Name);

        var windowsOnAlpha = UpdateAssetSelector.Select(alpha, PlatformDetector.WinX64);
        Assert.Null(windowsOnAlpha);

        var stable = AppUpdateEvaluator.Evaluate("0.2.2", UpdateChannel.Stable, PlatformDetector.WinX64, releases);
        Assert.Equal(UpdateCheckStatus.UpToDate, stable.Status);
        Assert.False(stable.CanDownload);

        var alphaCurrent = AppUpdateEvaluator.Evaluate("0.3.0-alpha.1", UpdateChannel.Prerelease, PlatformDetector.LinuxX64, releases);
        Assert.Equal(UpdateCheckStatus.UpToDate, alphaCurrent.Status);

        var alpha2Build = AppUpdateEvaluator.Evaluate("0.3.0-alpha.2", UpdateChannel.Prerelease, PlatformDetector.LinuxX64, releases);
        Assert.Equal(UpdateCheckStatus.CurrentIsNewerThanChannel, alpha2Build.Status);
        Assert.False(alpha2Build.CanDownload);
    }

    [Fact]
    public async Task DownloadsAndParsesPublishedLinuxChecksumFile()
    {
        using var http = new HttpClient();
        var client = new GitHubReleaseClient(http);
        var releases = await client.ListReleasesAsync();
        var alpha = Assert.Single(releases, r => r.TagName == "v0.3.0-alpha.1");
        var linux = UpdateAssetSelector.Select(alpha, PlatformDetector.LinuxX64);
        Assert.NotNull(linux?.ChecksumFile);

        var dir = Path.Combine(Path.GetTempPath(), "sapgun-live-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var downloader = new UpdateDownloader(http, TimeSpan.FromSeconds(30));
            var path = Path.Combine(dir, linux!.ChecksumFile!.Name);
            await downloader.DownloadAsync(linux.ChecksumFile.BrowserDownloadUrl, path);
            var text = await File.ReadAllTextAsync(path);
            var hash = ChecksumParser.FindSha256(text, linux.Package.Name);
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.True(UpdateAssetSelector.IsSha256Hex(hash));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
