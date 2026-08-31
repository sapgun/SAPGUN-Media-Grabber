using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class WindowsInstallerRelaunchTests
{
    [Fact]
    public void CmdWaitsForInstallerThenStartsApp()
    {
        var args = WindowsInstallerRelaunch.CmdArguments(
            @"C:\Users\a\Downloads\SAPGUN-Media-Grabber-Setup.exe",
            @"C:\Users\a\AppData\Local\Programs\SAPGUN Media Grabber\SAPGUN Media Grabber.exe");
        Assert.StartsWith("/c start /wait", args, StringComparison.Ordinal);
        Assert.Contains("SAPGUN-Media-Grabber-Setup.exe", args, StringComparison.Ordinal);
        Assert.Contains("if exist", args, StringComparison.Ordinal);
        Assert.Contains("SAPGUN Media Grabber.exe", args, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultPathIsPerUserProgramsFolder()
    {
        var path = WindowsInstallerRelaunch.DefaultInstalledExePath();
        Assert.Contains("SAPGUN Media Grabber", path, StringComparison.Ordinal);
        Assert.EndsWith("SAPGUN Media Grabber.exe", path, StringComparison.Ordinal);
    }
}
