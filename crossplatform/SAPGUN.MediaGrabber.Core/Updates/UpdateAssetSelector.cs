using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SapgunMediaGrabber.Updates;

public static class UpdateAssetSelector
{
    public const string WindowsInstallerName = "SAPGUN-Media-Grabber-Setup.exe";
    public const string WindowsInstallerChecksumName = "SHA256SUMS-win-setup.txt";

    static readonly Regex WindowsZipPackage = new(@"^SAPGUN-Media-Grabber-v.+-windows-x64\.zip$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static SelectedUpdateAsset? Select(GitHubRelease release, string platform)
    {
        if (!PlatformDetector.IsSupported(platform)) return null;

        var package = platform == PlatformDetector.WinX64
            ? SelectWindowsPackage(release)
            : SelectNamedPackage(release, platform);
        if (package is null) return null;

        var checksumName = ChecksumNameForPackage(package.Name, platform);
        var checksum = checksumName == null
            ? null
            : release.Assets.FirstOrDefault(a => string.Equals(a.Name, checksumName, StringComparison.Ordinal));

        return new SelectedUpdateAsset
        {
            Package = package,
            ChecksumFile = checksum,
            ExpectedSha256 = DigestSha256(package.Digest),
            Platform = platform,
            ApplyAction = package.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? UpdateApplyAction.LaunchInstallerAndExit
                : UpdateApplyAction.RevealDownload
        };
    }

    static GitHubAsset? SelectWindowsPackage(GitHubRelease release)
    {
        var zipName = ExpectedWindowsZipName(release.TagName);
        GitHubAsset? zip = null;
        GitHubAsset? installer = null;
        foreach (var asset in release.Assets)
        {
            if (string.Equals(asset.Name, zipName, StringComparison.Ordinal) && WindowsZipPackage.IsMatch(asset.Name))
            {
                if (zip != null) return null;
                zip = asset;
            }
            else if (string.Equals(asset.Name, WindowsInstallerName, StringComparison.Ordinal))
            {
                if (installer != null) return null;
                installer = asset;
            }
        }

        // Stable / Latest Windows upgrades use Setup.exe. Prerelease keeps the portable zip first.
        if (release.Prerelease)
            return zip ?? installer;
        return installer ?? zip;
    }

    static GitHubAsset? SelectNamedPackage(GitHubRelease release, string platform)
    {
        var expectedName = ExpectedPackageName(release.TagName, platform);
        var tarball = UniqueNamedAsset(release, expectedName);
        if (DuplicateNamedAsset(release, expectedName)) return null;
        if (tarball != null) return tarball;

        var appImageName = ExpectedAppImageName(release.TagName, platform);
        if (appImageName is null) return null;
        if (DuplicateNamedAsset(release, appImageName)) return null;
        return UniqueNamedAsset(release, appImageName);
    }

    static GitHubAsset? UniqueNamedAsset(GitHubRelease release, string name)
    {
        GitHubAsset? found = null;
        foreach (var asset in release.Assets)
        {
            if (!string.Equals(asset.Name, name, StringComparison.Ordinal)) continue;
            if (found != null) return null;
            found = asset;
        }
        return found;
    }

    static bool DuplicateNamedAsset(GitHubRelease release, string name)
    {
        var count = 0;
        foreach (var asset in release.Assets)
        {
            if (!string.Equals(asset.Name, name, StringComparison.Ordinal)) continue;
            count++;
            if (count > 1) return true;
        }
        return false;
    }

    public static string ExpectedWindowsZipName(string tagName)
    {
        var tag = NormalizeTag(tagName);
        return $"SAPGUN-Media-Grabber-{tag}-windows-x64.zip";
    }

    public static string ExpectedPackageName(string tagName, string platform)
    {
        var tag = NormalizeTag(tagName);
        return platform switch
        {
            PlatformDetector.WinX64 => ExpectedWindowsZipName(tag),
            PlatformDetector.LinuxX64 => $"SAPGUN-Media-Grabber-{tag}-linux-x64.tar.gz",
            PlatformDetector.LinuxArm64 => $"SAPGUN-Media-Grabber-{tag}-linux-arm64.tar.gz",
            PlatformDetector.OsxArm64 => $"SAPGUN-Media-Grabber-{tag}-macos-arm64.tar.gz",
            PlatformDetector.OsxX64 => $"SAPGUN-Media-Grabber-{tag}-macos-x64.tar.gz",
            _ => ""
        };
    }

    static string NormalizeTag(string tagName)
    {
        var tag = tagName.Trim();
        if (!tag.StartsWith('v')) tag = "v" + tag;
        return tag;
    }

    public static string? ExpectedAppImageName(string tagName, string platform)
    {
        var tag = NormalizeTag(tagName);
        return platform switch
        {
            PlatformDetector.LinuxX64 => $"SAPGUN-Media-Grabber-{tag}-linux-x64.AppImage",
            PlatformDetector.LinuxArm64 => $"SAPGUN-Media-Grabber-{tag}-linux-arm64.AppImage",
            _ => null
        };
    }

    public static string? ExpectedChecksumName(string platform) => platform switch
    {
        PlatformDetector.WinX64 => "SHA256SUMS-win-x64.txt",
        PlatformDetector.LinuxX64 => "SHA256SUMS-linux-x64.txt",
        PlatformDetector.LinuxArm64 => "SHA256SUMS-linux-arm64.txt",
        PlatformDetector.OsxArm64 => "SHA256SUMS-macos-arm64.txt",
        PlatformDetector.OsxX64 => "SHA256SUMS-macos-x64.txt",
        _ => null
    };

    public static string? ChecksumNameForPackage(string packageName, string platform)
    {
        if (string.Equals(packageName, WindowsInstallerName, StringComparison.OrdinalIgnoreCase))
            return WindowsInstallerChecksumName;
        if (packageName.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
            return platform switch
            {
                PlatformDetector.LinuxX64 => "SHA256SUMS-linux-x64-appimage.txt",
                PlatformDetector.LinuxArm64 => "SHA256SUMS-linux-arm64-appimage.txt",
                _ => null
            };
        return ExpectedChecksumName(platform);
    }

    public static string? DigestSha256(string? digest)
    {
        if (string.IsNullOrWhiteSpace(digest)) return null;
        const string prefix = "sha256:";
        if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var hex = digest[prefix.Length..].Trim();
        return IsSha256Hex(hex) ? hex.ToLowerInvariant() : null;
    }

    public static bool IsSha256Hex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
        return value.All(Uri.IsHexDigit);
    }
}

public static class ChecksumParser
{
    public static string? FindSha256(string checksumFileText, string assetFileName)
    {
        if (string.IsNullOrWhiteSpace(checksumFileText) || string.IsNullOrWhiteSpace(assetFileName))
            return null;

        foreach (var raw in checksumFileText.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            var hash = parts[0].ToLowerInvariant();
            if (!UpdateAssetSelector.IsSha256Hex(hash)) continue;
            var name = parts[1].TrimStart('*').Replace('\\', '/');
            if (string.Equals(Path.GetFileName(name), assetFileName, StringComparison.Ordinal)
                || string.Equals(name, assetFileName, StringComparison.Ordinal))
                return hash;
        }
        return null;
    }
}

public static class Sha256Verifier
{
    public static async Task<string> ComputeFileSha256Async(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static void EnsureMatch(string actual, string expected, string filePath)
    {
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(filePath); } catch { /* best-effort cleanup */ }
            throw new InvalidDataException("SHA-256 verification failed. The downloaded file was deleted.");
        }
    }
}
