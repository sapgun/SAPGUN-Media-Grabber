namespace SapgunMediaGrabber.Updates;

public enum UpdateChannel
{
    Stable,
    Prerelease
}

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    NoReleaseForChannel,
    NoCompatibleAsset,
    CurrentIsNewerThanChannel,
    InvalidCurrentVersion,
    CheckFailed
}

public enum UpdateApplyAction
{
    LaunchInstallerAndExit,
    RevealDownload
}

public sealed class GitHubAsset
{
    public required string Name { get; init; }
    public required string BrowserDownloadUrl { get; init; }
    public string? Digest { get; init; }
    public long Size { get; init; }
}

public sealed class GitHubRelease
{
    public required string TagName { get; init; }
    public required string HtmlUrl { get; init; }
    public string Name { get; init; } = "";
    public string Body { get; init; } = "";
    public bool Draft { get; init; }
    public bool Prerelease { get; init; }
    public IReadOnlyList<GitHubAsset> Assets { get; init; } = Array.Empty<GitHubAsset>();
}

public sealed class SelectedUpdateAsset
{
    public required GitHubAsset Package { get; init; }
    public GitHubAsset? ChecksumFile { get; init; }
    public string? ExpectedSha256 { get; init; }
    public required string Platform { get; init; }
    public required UpdateApplyAction ApplyAction { get; init; }
}

public sealed class UpdateCheckResult
{
    public required UpdateCheckStatus Status { get; init; }
    public required string CurrentVersion { get; init; }
    public required UpdateChannel Channel { get; init; }
    public required string Platform { get; init; }
    public string? LatestVersion { get; init; }
    public string? ReleaseNotes { get; init; }
    public string? ReleaseUrl { get; init; }
    public bool LatestIsPrerelease { get; init; }
    public SelectedUpdateAsset? Asset { get; init; }
    public string Message { get; init; } = "";

    public bool CanDownload => Status == UpdateCheckStatus.UpdateAvailable && Asset != null;
}

public sealed class UpdateDownloadProgress
{
    public long BytesReceived { get; init; }
    public long? TotalBytes { get; init; }
    public int Percent { get; init; }
}

public sealed class DownloadedUpdate
{
    public required string FilePath { get; init; }
    public required string Sha256 { get; init; }
    public required bool ChecksumVerified { get; init; }
    public required UpdateApplyAction ApplyAction { get; init; }
    public string? ExpectedSha256 { get; init; }
}
