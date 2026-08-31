using System.Text;
using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class AppUpdateEvaluatorTests
{
    static GitHubRelease Rel(string tag, bool pre, bool draft, params string[] assetNames) => new()
    {
        TagName = tag,
        HtmlUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/tag/" + tag,
        Prerelease = pre,
        Draft = draft,
        Body = "Notes for " + tag,
        Assets = assetNames.Select(name => new GitHubAsset
        {
            Name = name,
            BrowserDownloadUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/download/" + tag + "/" + name,
            Digest = "sha256:" + new string('e', 64)
        }).ToList()
    };

    [Fact]
    public void StableChannelIgnoresPrereleases()
    {
        var result = AppUpdateEvaluator.Evaluate("0.2.2", UpdateChannel.Stable, PlatformDetector.WinX64, new[]
        {
            Rel("v0.3.0-alpha.1", true, false, "SAPGUN-Media-Grabber-v0.3.0-alpha.1-linux-x64.tar.gz"),
            Rel("v0.2.2", false, false, "SAPGUN-Media-Grabber-Setup.exe")
        });
        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
        Assert.Equal("0.2.2", result.LatestVersion);
        Assert.False(result.CanDownload);
    }

    [Fact]
    public void PrereleaseChannelSelectsNewerAlpha()
    {
        var result = AppUpdateEvaluator.Evaluate("0.3.0-alpha.1", UpdateChannel.Prerelease, PlatformDetector.LinuxX64, new[]
        {
            Rel("v0.2.2", false, false, "SAPGUN-Media-Grabber-Setup.exe"),
            Rel("v0.3.0-alpha.2", true, false, "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz", "SHA256SUMS-linux-x64.txt")
        });
        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.True(result.CanDownload);
        Assert.Equal("0.3.0-alpha.2", result.LatestVersion);
        Assert.Equal("SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz", result.Asset!.Package.Name);
    }

    [Fact]
    public void AlphaUserOnStableIsNewerThanChannel()
    {
        var result = AppUpdateEvaluator.Evaluate("0.3.0-alpha.1", UpdateChannel.Stable, PlatformDetector.LinuxX64, new[]
        {
            Rel("v0.3.0-alpha.1", true, false, "SAPGUN-Media-Grabber-v0.3.0-alpha.1-linux-x64.tar.gz"),
            Rel("v0.2.2", false, false, "SAPGUN-Media-Grabber-Setup.exe")
        });
        Assert.Equal(UpdateCheckStatus.CurrentIsNewerThanChannel, result.Status);
        Assert.False(result.CanDownload);
    }

    [Fact]
    public void MissingPlatformAssetIsNotDownloadable()
    {
        var result = AppUpdateEvaluator.Evaluate("0.3.0-alpha.1", UpdateChannel.Prerelease, PlatformDetector.WinX64, new[]
        {
            Rel("v0.3.0-alpha.2", true, false, "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz")
        });
        Assert.Equal(UpdateCheckStatus.NoCompatibleAsset, result.Status);
        Assert.False(result.CanDownload);
    }

    [Fact]
    public void SkipsDrafts()
    {
        var result = AppUpdateEvaluator.Evaluate("0.3.0-alpha.1", UpdateChannel.Prerelease, PlatformDetector.LinuxX64, new[]
        {
            Rel("v0.3.0-alpha.2", true, true, "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz"),
            Rel("v0.3.0-alpha.1", true, false, "SAPGUN-Media-Grabber-v0.3.0-alpha.1-linux-x64.tar.gz")
        });
        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
    }

    [Fact]
    public void RejectsInvalidCurrentVersion()
    {
        var result = AppUpdateEvaluator.Evaluate("not-a-version", UpdateChannel.Prerelease, PlatformDetector.LinuxX64, Array.Empty<GitHubRelease>());
        Assert.Equal(UpdateCheckStatus.InvalidCurrentVersion, result.Status);
    }
}

public class GitHubReleaseClientTests
{
    [Fact]
    public void ParsesValidReleaseArray()
    {
        var json = """
        [
          {
            "tag_name": "v0.3.0-alpha.1",
            "html_url": "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/tag/v0.3.0-alpha.1",
            "prerelease": true,
            "draft": false,
            "body": "alpha",
            "assets": [
              {
                "name": "SAPGUN-Media-Grabber-v0.3.0-alpha.1-linux-x64.tar.gz",
                "browser_download_url": "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/download/v0.3.0-alpha.1/SAPGUN-Media-Grabber-v0.3.0-alpha.1-linux-x64.tar.gz",
                "digest": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "size": 10
              }
            ]
          }
        ]
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var releases = GitHubReleaseClient.ParseReleases(stream);
        Assert.Single(releases);
        Assert.Equal("v0.3.0-alpha.1", releases[0].TagName);
        Assert.Single(releases[0].Assets);
    }

    [Fact]
    public void DropsAssetsFromWrongRepositoryOrHttp()
    {
        var json = """
        [
          {
            "tag_name": "v1.0.0",
            "html_url": "https://github.com/evil/repo/releases/tag/v1.0.0",
            "assets": []
          },
          {
            "tag_name": "v0.2.2",
            "html_url": "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/tag/v0.2.2",
            "assets": [
              {
                "name": "SAPGUN-Media-Grabber-Setup.exe",
                "browser_download_url": "http://github.com/sapgun/SAPGUN-Media-Grabber/releases/download/v0.2.2/SAPGUN-Media-Grabber-Setup.exe"
              }
            ]
          }
        ]
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var releases = GitHubReleaseClient.ParseReleases(stream);
        Assert.Single(releases);
        Assert.Empty(releases[0].Assets);
    }

    [Fact]
    public void RejectsNonArrayPayload()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"tag_name":"v1"}"""));
        Assert.Throws<InvalidDataException>(() => GitHubReleaseClient.ParseReleases(stream));
    }

    [Fact]
    public void RejectsMalformedJson()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{not-json"));
        Assert.Throws<InvalidDataException>(() => GitHubReleaseClient.ParseReleases(stream));
    }

    [Fact]
    public void SkipsReleasesMissingTag()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""[{"html_url":"https://github.com/sapgun/SAPGUN-Media-Grabber/releases/tag/x"}]"""));
        Assert.Empty(GitHubReleaseClient.ParseReleases(stream));
    }
}

public class AppUpdateServiceNetworkTests
{
    [Fact]
    public async Task CheckFailedOnHttpError()
    {
        var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)))
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var service = new AppUpdateService(http);
        var result = await service.CheckAsync("0.3.0-alpha.1", UpdateChannel.Prerelease, PlatformDetector.LinuxX64);
        Assert.Equal(UpdateCheckStatus.CheckFailed, result.Status);
        Assert.Contains("GitHub", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadDeletesFileOnChecksumMismatch()
    {
        var payload = "not-the-expected-bytes"u8.ToArray();
        var handler = new StubHandler(req =>
        {
            var name = Path.GetFileName(req.RequestUri!.AbsolutePath);
            if (name == "SHA256SUMS-linux-x64.txt")
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent($"{new string('a', 64)}  SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz")
                };
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            };
        });
        var service = new AppUpdateService(new HttpClient(handler));
        var check = AppUpdateEvaluator.Evaluate("0.3.0-alpha.1", UpdateChannel.Prerelease, PlatformDetector.LinuxX64, new[]
        {
            new GitHubRelease
            {
                TagName = "v0.3.0-alpha.2",
                HtmlUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/tag/v0.3.0-alpha.2",
                Prerelease = true,
                Assets = new[]
                {
                    new GitHubAsset
                    {
                        Name = "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz",
                        BrowserDownloadUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/download/v0.3.0-alpha.2/SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz"
                    },
                    new GitHubAsset
                    {
                        Name = "SHA256SUMS-linux-x64.txt",
                        BrowserDownloadUrl = "https://github.com/sapgun/SAPGUN-Media-Grabber/releases/download/v0.3.0-alpha.2/SHA256SUMS-linux-x64.txt"
                    }
                }
            }
        });
        var dir = Path.Combine(Path.GetTempPath(), "sapgun-update-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(check, dir));
            Assert.False(File.Exists(Path.Combine(dir, "SAPGUN-Media-Grabber-v0.3.0-alpha.2-linux-x64.tar.gz")));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    sealed class StubHandler : HttpMessageHandler
    {
        readonly Func<HttpRequestMessage, HttpResponseMessage> reply;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) => this.reply = reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(reply(request));
    }
}
