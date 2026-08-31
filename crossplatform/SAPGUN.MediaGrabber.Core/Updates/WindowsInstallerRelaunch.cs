namespace SapgunMediaGrabber.Updates;

public static class WindowsInstallerRelaunch
{
    public const string InstallFolderName = "SAPGUN Media Grabber";
    public const string ExeName = "SAPGUN Media Grabber.exe";

    public static string DefaultInstalledExePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", InstallFolderName, ExeName);

    public static string CmdArguments(string installerPath, string appPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(appPath);
        return "/c start /wait \"\" \"" + installerPath + "\" & if exist \"" + appPath + "\" start \"\" \"" + appPath + "\"";
    }
}
