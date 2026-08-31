using System.Net.Http.Headers;
using System.Text.Json;

namespace SapgunMediaGrabber.Updates;

public sealed class GitHubReleaseClient
{
    public const string DefaultOwner = "sapgun";
    public const string DefaultRepo = "SAPGUN-Media-Grabber";

    readonly HttpClient http;
    readonly string owner;
    readonly string repo;

    public GitHubReleaseClient(HttpClient http, string owner = DefaultOwner, string repo = DefaultRepo)
    {
        this.http = http ?? throw new ArgumentNullException(nameof(http));
        this.owner = owner;
        this.repo = repo;
        EnsureUserAgent(this.http);
    }

    public static void EnsureUserAgent(HttpClient client)
    {
        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SAPGUN-Media-Grabber", "app-updater"));
        if (!client.DefaultRequestHeaders.Accept.Any())
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        ApplyOptionalToken(client);
    }

    public static void ApplyOptionalToken(HttpClient client)
    {
        if (client.DefaultRequestHeaders.Authorization != null) return;
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? Environment.GetEnvironmentVariable("GH_TOKEN");
        if (string.IsNullOrWhiteSpace(token)) return;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
    }

    public async Task<IReadOnlyList<GitHubRelease>> ListReleasesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=40";
        using var response = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"GitHub releases request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return ParseReleases(stream, owner, repo);
    }

    public static IReadOnlyList<GitHubRelease> ParseReleases(Stream json, string owner = DefaultOwner, string repo = DefaultRepo)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GitHub returned malformed JSON.", ex);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("GitHub releases response was not a JSON array.");

            var list = new List<GitHubRelease>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                if (!TryReadRelease(item, owner, repo, out var release)) continue;
                list.Add(release);
            }
            return list;
        }
    }

    static bool TryReadRelease(JsonElement item, string owner, string repo, out GitHubRelease release)
    {
        release = null!;
        var tag = ReadString(item, "tag_name");
        if (string.IsNullOrWhiteSpace(tag)) return false;

        var htmlUrl = ReadString(item, "html_url");
        var expectedPrefix = $"https://github.com/{owner}/{repo}/";
        if (string.IsNullOrWhiteSpace(htmlUrl) ||
            !htmlUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var assets = new List<GitHubAsset>();
        if (item.TryGetProperty("assets", out var assetsEl) && assetsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assetsEl.EnumerateArray())
            {
                if (asset.ValueKind != JsonValueKind.Object) continue;
                var name = ReadString(asset, "name");
                var url = ReadString(asset, "browser_download_url");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;
                if (!IsAllowedDownloadUrl(url, owner, repo)) continue;
                assets.Add(new GitHubAsset
                {
                    Name = name,
                    BrowserDownloadUrl = url,
                    Digest = ReadString(asset, "digest"),
                    Size = asset.TryGetProperty("size", out var sizeEl) && sizeEl.TryGetInt64(out var size) ? size : 0
                });
            }
        }

        release = new GitHubRelease
        {
            TagName = tag,
            HtmlUrl = htmlUrl,
            Name = ReadString(item, "name") ?? "",
            Body = ReadString(item, "body") ?? "",
            Draft = item.TryGetProperty("draft", out var draftEl) && draftEl.ValueKind == JsonValueKind.True,
            Prerelease = item.TryGetProperty("prerelease", out var preEl) && preEl.ValueKind == JsonValueKind.True,
            Assets = assets
        };
        return true;
    }

    public static bool IsAllowedDownloadUrl(string url, string owner = DefaultOwner, string repo = DefaultRepo)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;

        if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            return uri.AbsolutePath.StartsWith($"/{owner}/{repo}/releases/download/", StringComparison.OrdinalIgnoreCase);

        return uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    static string? ReadString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }
}
