using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using SapgunMediaGrabber.Updates;

namespace SapgunMediaGrabber;

public static class AppVersionInfo
{
    public static string Current
    {
        get
        {
            var raw = typeof(AppVersionInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (string.IsNullOrWhiteSpace(raw)) return "0.0.0";
            var plus = raw.IndexOf('+');
            return plus >= 0 ? raw[..plus] : raw;
        }
    }

    public static UpdateChannel DefaultChannel =>
        SemVersion.TryParse(Current, out var version) && version.IsPrerelease
            ? UpdateChannel.Prerelease
            : UpdateChannel.Stable;
}

public static class UpdateShell
{
    public static void Reveal(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                OpenPath(dir);
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + filePath + "\"") { UseShellExecute = true });
                return;
            }
            if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo("open") { ArgumentList = { "-R", filePath } });
                return;
            }
            var folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder)) OpenPath(folder);
        }
        catch { }
    }

    public static void LaunchInstaller(string filePath)
    {
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    public static void ShutdownApp()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    static void OpenPath(string target)
    {
        if (OperatingSystem.IsMacOS()) Process.Start("open", new[] { target });
        else if (OperatingSystem.IsLinux()) Process.Start("xdg-open", new[] { target });
        else Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }
}
