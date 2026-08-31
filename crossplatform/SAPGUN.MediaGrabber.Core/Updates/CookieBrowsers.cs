namespace SapgunMediaGrabber.Updates;

public sealed record CookieBrowser(string Id, string Label);

public static class CookieBrowsers
{
    public static readonly CookieBrowser None = new("", "None (public media)");

    public static IReadOnlyList<CookieBrowser> ForCurrentOs()
    {
        if (OperatingSystem.IsMacOS()) return ForPlatform(PlatformDetector.OsxArm64);
        if (OperatingSystem.IsWindows()) return ForPlatform(PlatformDetector.WinX64);
        return ForPlatform(PlatformDetector.LinuxX64);
    }

    public static IReadOnlyList<CookieBrowser> ForPlatform(string platform)
    {
        var list = new List<CookieBrowser> { None };
        if (platform == PlatformDetector.OsxArm64 || platform.StartsWith("osx-", StringComparison.Ordinal))
        {
            list.Add(new CookieBrowser("safari", "Safari"));
            list.Add(new CookieBrowser("chrome", "Chrome"));
            list.Add(new CookieBrowser("firefox", "Firefox"));
            list.Add(new CookieBrowser("brave", "Brave"));
            list.Add(new CookieBrowser("edge", "Edge"));
            return list;
        }

        if (platform == PlatformDetector.WinX64 || platform.StartsWith("win-", StringComparison.Ordinal))
        {
            list.Add(new CookieBrowser("edge", "Edge"));
            list.Add(new CookieBrowser("chrome", "Chrome"));
            list.Add(new CookieBrowser("brave", "Brave"));
            list.Add(new CookieBrowser("firefox", "Firefox"));
            return list;
        }

        list.Add(new CookieBrowser("firefox", "Firefox"));
        list.Add(new CookieBrowser("chrome", "Chrome"));
        list.Add(new CookieBrowser("chromium", "Chromium"));
        list.Add(new CookieBrowser("brave", "Brave"));
        list.Add(new CookieBrowser("edge", "Edge"));
        return list;
    }

    public static string Hint(string platform)
    {
        if (platform == PlatformDetector.OsxArm64 || platform.StartsWith("osx-", StringComparison.Ordinal))
            return "403 / sign-in errors? Pick Safari, Chrome, Firefox, Brave, or Edge where you are already signed in. Cookie extraction on macOS is still alpha.";
        if (platform == PlatformDetector.WinX64 || platform.StartsWith("win-", StringComparison.Ordinal))
            return "403 / sign-in errors? Pick the Windows browser where you are already signed in.";
        return "403 / sign-in errors? Pick Firefox, Chrome, Chromium, Brave, or Edge. Safari is not available on Linux.";
    }
}
