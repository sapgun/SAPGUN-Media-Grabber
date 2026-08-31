using System.Runtime.InteropServices;

namespace SapgunMediaGrabber.Updates;

public static class PlatformDetector
{
    public const string WinX64 = "win-x64";
    public const string LinuxX64 = "linux-x64";
    public const string OsxArm64 = "osx-arm64";
    public const string OsxX64 = "osx-x64";

    public static string Detect()
    {
        var arch = RuntimeInformation.OSArchitecture;
        if (OperatingSystem.IsWindows() && arch == Architecture.X64) return WinX64;
        if (OperatingSystem.IsLinux() && arch == Architecture.X64) return LinuxX64;
        if (OperatingSystem.IsMacOS() && arch == Architecture.Arm64) return OsxArm64;
        if (OperatingSystem.IsMacOS() && arch == Architecture.X64) return OsxX64;

        var os = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsMacOS() ? "osx"
            : "unknown";
        var archName = arch.ToString().ToLowerInvariant();
        return os + "-" + archName;
    }

    public static bool IsSupported(string platform) =>
        platform is WinX64 or LinuxX64 or OsxArm64 or OsxX64;

    public static UpdateApplyAction ApplyAction(string platform) =>
        platform == WinX64 ? UpdateApplyAction.LaunchInstallerAndExit : UpdateApplyAction.RevealDownload;

    public static string DisplayName(string platform) => platform switch
    {
        WinX64 => "Windows x64",
        LinuxX64 => "Linux x64",
        OsxArm64 => "macOS Apple Silicon",
        OsxX64 => "macOS Intel",
        _ => platform
    };
}
