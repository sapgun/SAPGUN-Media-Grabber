using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class CookieBrowsersTests
{
    [Fact]
    public void LinuxDoesNotOfferSafari()
    {
        var ids = CookieBrowsers.ForPlatform(PlatformDetector.LinuxX64).Select(b => b.Id).ToArray();
        Assert.DoesNotContain("safari", ids);
        Assert.Contains("chromium", ids);
        Assert.Contains("firefox", ids);
        Assert.Equal("", ids[0]);
    }

    [Fact]
    public void MacOsOffersSafariFirstAfterNone()
    {
        var ids = CookieBrowsers.ForPlatform(PlatformDetector.OsxArm64).Select(b => b.Id).ToArray();
        Assert.Equal(new[] { "", "safari", "chrome", "firefox", "brave", "edge" }, ids);
        Assert.DoesNotContain("chromium", ids);
    }

    [Fact]
    public void WindowsMatchesStableWinFormsSet()
    {
        var ids = CookieBrowsers.ForPlatform(PlatformDetector.WinX64).Select(b => b.Id).ToArray();
        Assert.Equal(new[] { "", "edge", "chrome", "brave", "firefox" }, ids);
        Assert.DoesNotContain("safari", ids);
    }

    [Fact]
    public void YtDlpBrowserIdsAreLowercaseTokens()
    {
        foreach (var platform in new[] { PlatformDetector.LinuxX64, PlatformDetector.OsxArm64, PlatformDetector.WinX64 })
        {
            foreach (var browser in CookieBrowsers.ForPlatform(platform).Where(b => b.Id != ""))
            {
                Assert.Matches("^[a-z]+$", browser.Id);
                Assert.DoesNotContain(' ', browser.Id);
            }
        }
    }
}
