namespace SapgunMediaGrabber.Updates;

public sealed class AppUpdateService
{
    readonly GitHubReleaseClient releases;
    readonly UpdateDownloader downloader;

    public AppUpdateService(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        http.Timeout = Timeout.InfiniteTimeSpan;
        releases = new GitHubReleaseClient(http);
        downloader = new UpdateDownloader(http);
    }

    public async Task<UpdateCheckResult> CheckAsync(
        string currentVersion,
        UpdateChannel channel,
        string? platform = null,
        CancellationToken cancellationToken = default)
    {
        platform ??= PlatformDetector.Detect();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var list = await releases.ListReleasesAsync(timeout.Token).ConfigureAwait(false);
            return AppUpdateEvaluator.Evaluate(currentVersion, channel, platform, list);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Fail(currentVersion, channel, platform, "The GitHub update check timed out.");
        }
        catch (HttpRequestException ex)
        {
            return Fail(currentVersion, channel, platform, "Could not reach GitHub Releases. " + ex.Message);
        }
        catch (InvalidDataException)
        {
            return Fail(currentVersion, channel, platform, "GitHub returned an unexpected releases payload.");
        }
        catch (Exception ex)
        {
            return Fail(currentVersion, channel, platform, "App update check failed. " + ex.Message);
        }
    }

    public async Task<DownloadedUpdate> DownloadAsync(
        UpdateCheckResult check,
        string destinationDirectory,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!check.CanDownload || check.Asset is null)
            throw new InvalidOperationException("No verified update package is available to download.");

        Directory.CreateDirectory(destinationDirectory);
        var asset = check.Asset;
        var dest = Path.Combine(destinationDirectory, asset.Package.Name);

        string? expected = asset.ExpectedSha256;
        if (asset.ChecksumFile != null)
        {
            var checksumPath = Path.Combine(destinationDirectory, asset.ChecksumFile.Name);
            await downloader.DownloadAsync(asset.ChecksumFile.BrowserDownloadUrl, checksumPath, null, cancellationToken).ConfigureAwait(false);
            var text = await File.ReadAllTextAsync(checksumPath, cancellationToken).ConfigureAwait(false);
            var fromFile = ChecksumParser.FindSha256(text, asset.Package.Name);
            if (fromFile is null)
                throw new InvalidDataException("The checksum file did not list the selected update package.");
            if (expected != null && !string.Equals(expected, fromFile, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("GitHub digest and SHA256SUMS file do not match. Update aborted.");
            expected = fromFile;
        }

        await downloader.DownloadAsync(asset.Package.BrowserDownloadUrl, dest, progress, cancellationToken).ConfigureAwait(false);
        var actual = await Sha256Verifier.ComputeFileSha256Async(dest, cancellationToken).ConfigureAwait(false);
        if (expected != null)
            Sha256Verifier.EnsureMatch(actual, expected, dest);

        return new DownloadedUpdate
        {
            FilePath = dest,
            Sha256 = actual,
            ChecksumVerified = expected != null,
            ExpectedSha256 = expected,
            ApplyAction = asset.ApplyAction
        };
    }

    static UpdateCheckResult Fail(string current, UpdateChannel channel, string platform, string message) =>
        new()
        {
            Status = UpdateCheckStatus.CheckFailed,
            CurrentVersion = current,
            Channel = channel,
            Platform = platform,
            Message = message
        };
}
