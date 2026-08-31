namespace SapgunMediaGrabber.Updates;

public static class IncompleteDownloadCleanup
{
    public static int DeleteLeftovers(string directory, DateTimeOffset startedUtc)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return 0;

        var cutoff = startedUtc.AddSeconds(-2);
        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            if (!IsLeftoverName(Path.GetFileName(path))) continue;
            try
            {
                if (File.GetLastWriteTimeUtc(path) < cutoff.UtcDateTime) continue;
                File.Delete(path);
                deleted++;
            }
            catch { }
        }
        return deleted;
    }

    public static bool IsLeftoverName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        if (fileName.EndsWith(".part", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.Contains(".part-Frag", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.Contains(".f", StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
            return true;
        return fileName.EndsWith("_X.mp4", StringComparison.OrdinalIgnoreCase);
    }
}
