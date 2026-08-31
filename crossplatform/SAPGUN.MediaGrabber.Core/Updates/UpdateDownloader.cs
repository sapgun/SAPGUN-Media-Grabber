namespace SapgunMediaGrabber.Updates;

public sealed class UpdateDownloader
{
    readonly HttpClient http;
    readonly TimeSpan timeout;

    public UpdateDownloader(HttpClient http, TimeSpan? timeout = null)
    {
        this.http = http ?? throw new ArgumentNullException(nameof(http));
        this.timeout = timeout ?? TimeSpan.FromMinutes(10);
        GitHubReleaseClient.EnsureUserAgent(this.http);
    }

    public async Task<string> DownloadAsync(
        string url,
        string destinationPath,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!GitHubReleaseClient.IsAllowedDownloadUrl(url))
            throw new InvalidOperationException("Refusing to download from an unexpected URL.");

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var partial = destinationPath + ".partial";
        try
        {
            if (File.Exists(partial)) File.Delete(partial);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Download failed: {(int)response.StatusCode} {response.ReasonPhrase}");

            var total = response.Content.Headers.ContentLength;
            await using (var input = await response.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false))
            await using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81_920, useAsync: true))
            {
                var buffer = new byte[81_920];
                long received = 0;
                int read;
                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), timeoutCts.Token).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), timeoutCts.Token).ConfigureAwait(false);
                    received += read;
                    var percent = total is > 0 ? (int)Math.Clamp(received * 100 / total.Value, 0, 100) : 0;
                    progress?.Report(new UpdateDownloadProgress { BytesReceived = received, TotalBytes = total, Percent = percent });
                }
            }

            if (File.Exists(destinationPath)) File.Delete(destinationPath);
            File.Move(partial, destinationPath);
            return destinationPath;
        }
        catch
        {
            try { if (File.Exists(partial)) File.Delete(partial); } catch { }
            throw;
        }
    }
}
