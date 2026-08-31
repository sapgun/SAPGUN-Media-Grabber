using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SapgunMediaGrabber.Updates;

public static class UpdateAssetSelector
{
    public const string WindowsInstallerName = "SAPGUN-Media-Grabber-Setup.exe";

    static readonly Regex LinuxX64Package = new(@"^SAPGUN-Media-Grabber-v.+-linux-x64\.tar\.gz$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex LinuxArm64Package = new(@"^SAPGUN-Media-Grabber-v.+-linux-arm64\.tar\.gz$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex MacosArm64Package = new(@"^SAPGUN-Media-Grabber-v.+-macos-arm64\.tar\.gz$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex MacosX64Package = new(@"^SAPGUN-Media-Grabber-v.+-macos-x64\.tar\.gz$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    static readonly Regex WindowsZipPackage = new(@"^SAPGUN-Media-Grabber-v.+-windows-x64\.zip$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static SelectedUpdateAsset? Select(GitHubRelease release, string platform)
    {
        if (!PlatformDetector.IsSupported(platform)) return null;

        var package = platform == PlatformDetector.WinX64
            ? SelectWindowsPackage(release)
            : SelectNamedPackage(release, platform);
        if (package is null) return null;

        var checksumName = package.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? null
            : ExpectedChecksumName(platform);
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
        return zip ?? installer;
    }

    static GitHubAsset? SelectNamedPackage(GitHubRelease release, string platform)
    {
        var expectedName = ExpectedPackageName(release.TagName, platform);
        GitHubAsset? package = null;
        foreach (var asset in release.Assets)
        {
            if (!MatchesPackage(asset.Name, platform, expectedName)) continue;
            if (package != null) return null;
            package = asset;
        }
        return package;
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

    public static string? ExpectedChecksumName(string platform) => platform switch
    {
        PlatformDetector.WinX64 => "SHA256SUMS-win-x64.txt",
        PlatformDetector.LinuxX64 => "SHA256SUMS-linux-x64.txt",
        PlatformDetector.LinuxArm64 => "SHA256SUMS-linux-arm64.txt",
        PlatformDetector.OsxArm64 => "SHA256SUMS-macos-arm64.txt",
        PlatformDetector.OsxX64 => "SHA256SUMS-macos-x64.txt",
        _ => null
    };

    static bool MatchesPackage(string name, string platform, string expectedName)
    {
        if (string.Equals(name, expectedName, StringComparison.Ordinal)) return true;
        return platform switch
        {
            PlatformDetector.WinX64 => string.Equals(name, expectedName, StringComparison.Ordinal) || string.Equals(name, WindowsInstallerName, StringComparison.Ordinal),
            PlatformDetector.LinuxX64 => LinuxX64Package.IsMatch(name) && name.Equals(expectedName, StringComparison.Ordinal),
            PlatformDetector.LinuxArm64 => LinuxArm64Package.IsMatch(name) && name.Equals(expectedName, StringComparison.Ordinal),
            PlatformDetector.OsxArm64 => MacosArm64Package.IsMatch(name) && name.Equals(expectedName, StringComparison.Ordinal),
            PlatformDetector.OsxX64 => MacosX64Package.IsMatch(name) && name.Equals(expectedName, StringComparison.Ordinal),
            _ => false
        };
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
