using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class UpdateAssetSelectorTests
{
    static GitHubAsset Asset(string name, string? digest = null) => new()
    {
        Name = name,
        BrowserDownloadUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/download/v0.3.0-alpha.2/" + name,
        Digest = digest
    };

    static GitHubRelease Release(params GitHubAsset[] assets) => new()
    {
        TagName = "v0.3.0-alpha.2",
        HtmlUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/tag/v0.3.0-alpha.2",
        Prerelease = true,
        Assets = assets
    };

    [Fact]
    public void SelectsLinuxTarballAndChecksum()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz", "sha256:" + new string('a', 64)),
            Asset("SHA256SUMS-linux-x64.txt"),
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.2-macos-arm64.tar.gz")),
            PlatformDetector.LinuxX64);

        Assert.NotNull(selected);
        Assert.Equal("SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz", selected!.Package.Name);
        Assert.Equal("SHA256SUMS-linux-x64.txt", selected.ChecksumFile?.Name);
        Assert.Equal(new string('a', 64), selected.ExpectedSha256);
        Assert.Equal(UpdateApplyAction.RevealDownload, selected.ApplyAction);
    }

    [Fact]
    public void SelectsMacosArm64Tarball()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.2-macos-arm64.tar.gz"),
            Asset("SHA256SUMS-macos-arm64.txt")),
            PlatformDetector.OsxArm64);
        Assert.NotNull(selected);
        Assert.Equal("SAPGUN-Media-Grabber-v0.3.0-alpha.2-macos-arm64.tar.gz", selected!.Package.Name);
    }

    [Fact]
    public void SelectsMacosIntelTarball()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.2-macos-x64.tar.gz"),
            Asset("SHA256SUMS-macos-x64.txt")),
            PlatformDetector.OsxX64);
        Assert.NotNull(selected);
        Assert.Equal("SAPGUN-Media-Grabber-v0.3.0-alpha.2-macos-x64.tar.gz", selected!.Package.Name);
        Assert.Equal("SHA256SUMS-macos-x64.txt", selected.ChecksumFile?.Name);
        Assert.Equal(UpdateApplyAction.RevealDownload, selected.ApplyAction);
    }

    [Fact]
    public void SelectsWindowsInstallerWhenNoPortableZip()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("SAPGUN-Media-Grabber-Setup.exe"),
            Asset("notes.txt")),
            PlatformDetector.WinX64);
        Assert.NotNull(selected);
        Assert.Equal(UpdateAssetSelector.WindowsInstallerName, selected!.Package.Name);
        Assert.Equal(UpdateApplyAction.LaunchInstallerAndExit, selected.ApplyAction);
        Assert.Null(selected.ChecksumFile);
    }

    [Fact]
    public void PrefersWindowsPortableZipOverInstaller()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("SAPGUN-Media-Grabber-Setup.exe"),
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.2-windows-x64.zip", "sha256:" + new string('a', 64)),
            Asset("SHA256SUMS-win-x64.txt")),
            PlatformDetector.WinX64);

        Assert.NotNull(selected);
        Assert.Equal("SAPGUN-Media-Grabber-v0.3.0-alpha.2-windows-x64.zip", selected!.Package.Name);
        Assert.Equal("SHA256SUMS-win-x64.txt", selected.ChecksumFile?.Name);
        Assert.Equal(UpdateApplyAction.RevealDownload, selected.ApplyAction);
    }

    [Fact]
    public void RejectsFirstArbitraryAsset()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("random.zip"),
            Asset("source.tar.gz")),
            PlatformDetector.LinuxX64);
        Assert.Null(selected);
    }

    [Fact]
    public void RejectsWrongVersionedLinuxName()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.1-linux-x64.tar.gz")),
            PlatformDetector.LinuxX64);
        Assert.Null(selected);
    }

    [Fact]
    public void RejectsDuplicateMatchingAssets()
    {
        var selected = UpdateAssetSelector.Select(Release(
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz"),
            Asset("SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz")),
            PlatformDetector.LinuxX64);
        Assert.Null(selected);
    }

    [Fact]
    public void RejectsUnsupportedPlatform()
    {
        Assert.Null(UpdateAssetSelector.Select(Release(Asset("SAPGUN-Media-Grabber-Setup.exe")), "linux-arm64"));
        Assert.Null(UpdateAssetSelector.Select(Release(Asset("SAPGUN-Media-Grabber-Setup.exe")), "win-arm64"));
    }
}

public class ChecksumParserTests
{
    [Fact]
    public void ReadsGnuSha256SumLine()
    {
        var hash = new string('b', 64);
        var text = $"{hash}  SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz\n";
        Assert.Equal(hash, ChecksumParser.FindSha256(text, "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz"));
    }

    [Fact]
    public void MatchesBasenameWhenPathIsPresent()
    {
        var hash = new string('c', 64);
        var text = $"{hash}  dist/SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz";
        Assert.Equal(hash, ChecksumParser.FindSha256(text, "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz"));
    }

    [Fact]
    public void ReturnsNullWhenAssetMissing()
    {
        var text = $"{new string('d', 64)}  other.tar.gz";
        Assert.Null(ChecksumParser.FindSha256(text, "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz"));
    }
}

public class Sha256VerifierTests
{
    [Fact]
    public async Task EnsureMatchDeletesBadFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "sapgun-hash-" + Guid.NewGuid().ToString("N") + ".bin");
        await File.WriteAllTextAsync(path, "hello");
        Assert.Throws<InvalidDataException>(() => Sha256Verifier.EnsureMatch("aaa", "bbb", path));
        Assert.False(File.Exists(path));
    }
}
