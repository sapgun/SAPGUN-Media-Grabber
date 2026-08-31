namespace SapgunMediaGrabber.Updates;

public static class AppUpdateEvaluator
{
    public const int ReleaseNotesLimit = 900;

    public static UpdateCheckResult Evaluate(
        string currentVersion,
        UpdateChannel channel,
        string platform,
        IReadOnlyList<GitHubRelease> releases)
    {
        if (!SemVersion.TryParse(currentVersion, out var current))
        {
            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.InvalidCurrentVersion,
                CurrentVersion = currentVersion,
                Channel = channel,
                Platform = platform,
                Message = "This build has an invalid application version string."
            };
        }

        if (!PlatformDetector.IsSupported(platform))
        {
            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.NoCompatibleAsset,
                CurrentVersion = current.ToString(),
                Channel = channel,
                Platform = platform,
                Message = $"App updates are not published for {PlatformDetector.DisplayName(platform)} yet."
            };
        }

        var candidates = new List<(GitHubRelease Release, SemVersion Version)>();
        foreach (var release in releases)
        {
            if (release.Draft) continue;
            if (channel == UpdateChannel.Stable && release.Prerelease) continue;
            if (!SemVersion.TryParse(release.TagName, out var version)) continue;
            if (channel == UpdateChannel.Stable && version.IsPrerelease) continue;
            candidates.Add((release, version));
        }

        if (candidates.Count == 0)
        {
            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.NoReleaseForChannel,
                CurrentVersion = current.ToString(),
                Channel = channel,
                Platform = platform,
                Message = channel == UpdateChannel.Stable
                    ? "No stable SAPGUN Media Grabber release was found."
                    : "No prerelease was found on GitHub Releases."
            };
        }

        var ranked = candidates.OrderByDescending(c => c.Version).ToList();
        var withAsset = ranked
            .Select(c => (c.Release, c.Version, Asset: UpdateAssetSelector.Select(c.Release, platform)))
            .FirstOrDefault(c => c.Asset != null);

        if (withAsset.Release is null)
        {
            var newest = ranked[0];
            var notes = TruncateNotes(newest.Release.Body);
            if (newest.Version < current)
            {
                return new UpdateCheckResult
                {
                    Status = UpdateCheckStatus.CurrentIsNewerThanChannel,
                    CurrentVersion = current.ToString(),
                    Channel = channel,
                    Platform = platform,
                    LatestVersion = newest.Version.ToString(),
                    LatestIsPrerelease = newest.Release.Prerelease || newest.Version.IsPrerelease,
                    ReleaseNotes = notes,
                    ReleaseUrl = newest.Release.HtmlUrl,
                    Message = channel == UpdateChannel.Stable
                        ? $"You are on {current} which is newer than the latest stable release ({newest.Version}). Stay on the Prerelease channel to receive alpha/beta builds."
                        : $"This build ({current}) is newer than the latest listed release ({newest.Version})."
                };
            }

            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.NoCompatibleAsset,
                CurrentVersion = current.ToString(),
                Channel = channel,
                Platform = platform,
                LatestVersion = newest.Version.ToString(),
                LatestIsPrerelease = newest.Release.Prerelease || newest.Version.IsPrerelease,
                ReleaseNotes = notes,
                ReleaseUrl = newest.Release.HtmlUrl,
                Message = $"{newest.Version} is available, but it does not include a {PlatformDetector.DisplayName(platform)} package."
            };
        }

        var notesForAsset = TruncateNotes(withAsset.Release.Body);
        if (withAsset.Version < current)
        {
            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.CurrentIsNewerThanChannel,
                CurrentVersion = current.ToString(),
                Channel = channel,
                Platform = platform,
                LatestVersion = withAsset.Version.ToString(),
                LatestIsPrerelease = withAsset.Release.Prerelease || withAsset.Version.IsPrerelease,
                ReleaseNotes = notesForAsset,
                ReleaseUrl = withAsset.Release.HtmlUrl,
                Message = channel == UpdateChannel.Stable
                    ? $"You are on {current} which is newer than the latest stable release ({withAsset.Version}). Stay on the Prerelease channel to receive alpha/beta builds."
                    : $"This build ({current}) is newer than the latest listed release ({withAsset.Version})."
            };
        }

        if (withAsset.Version.CompareTo(current) == 0)
        {
            return new UpdateCheckResult
            {
                Status = UpdateCheckStatus.UpToDate,
                CurrentVersion = current.ToString(),
                Channel = channel,
                Platform = platform,
                LatestVersion = withAsset.Version.ToString(),
                LatestIsPrerelease = withAsset.Release.Prerelease || withAsset.Version.IsPrerelease,
                ReleaseNotes = notesForAsset,
                ReleaseUrl = withAsset.Release.HtmlUrl,
                Message = "SAPGUN Media Grabber is up to date."
            };
        }

        return new UpdateCheckResult
        {
            Status = UpdateCheckStatus.UpdateAvailable,
            CurrentVersion = current.ToString(),
            Channel = channel,
            Platform = platform,
            LatestVersion = withAsset.Version.ToString(),
            LatestIsPrerelease = withAsset.Release.Prerelease || withAsset.Version.IsPrerelease,
            ReleaseNotes = notesForAsset,
            ReleaseUrl = withAsset.Release.HtmlUrl,
            Asset = withAsset.Asset,
            Message = $"Update available: {current} → {withAsset.Version}."
        };
    }

    static string TruncateNotes(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";
        var text = body.Replace("\r\n", "\n").Trim();
        if (text.Length <= ReleaseNotesLimit) return text;
        return text[..ReleaseNotesLimit].TrimEnd() + "…";
    }
}
