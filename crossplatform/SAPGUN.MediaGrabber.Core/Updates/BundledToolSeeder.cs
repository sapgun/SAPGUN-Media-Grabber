namespace SapgunMediaGrabber.Updates;

public static class BundledToolSeeder
{
    public static int Seed(string seedDir, string toolsDir)
    {
        if (string.IsNullOrWhiteSpace(seedDir) || !Directory.Exists(seedDir)) return 0;
        Directory.CreateDirectory(toolsDir);

        var copied = 0;
        foreach (var source in Directory.EnumerateFiles(seedDir))
        {
            var name = Path.GetFileName(source);
            if (!IsBundledTool(name)) continue;
            var target = Path.Combine(toolsDir, name);
            if (IsYtDlp(name) && File.Exists(target)) continue;
            if (File.Exists(target) && File.GetLastWriteTimeUtc(source) <= File.GetLastWriteTimeUtc(target))
                continue;
            File.Copy(source, target, overwrite: true);
            EnsureExecutable(target);
            copied++;
        }
        return copied;
    }

    public static bool IsBundledTool(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        return stem.Equals("yt-dlp", StringComparison.OrdinalIgnoreCase)
            || stem.Equals("ffmpeg", StringComparison.OrdinalIgnoreCase)
            || stem.Equals("ffprobe", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsYtDlp(string fileName) =>
        Path.GetFileNameWithoutExtension(fileName).Equals("yt-dlp", StringComparison.OrdinalIgnoreCase);

    static void EnsureExecutable(string path)
    {
        if (OperatingSystem.IsWindows() || !File.Exists(path)) return;
        try
        {
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch { }
    }
}
