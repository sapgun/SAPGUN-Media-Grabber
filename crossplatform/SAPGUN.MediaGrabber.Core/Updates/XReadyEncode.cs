namespace SapgunMediaGrabber.Updates;

public static class XReadyEncode
{
    public const string VideoCodec = "libx264";
    public const string AudioCodec = "aac";
    public const string PixelFormat = "yuv420p";
    public const string Profile = "high";
    public const string Level = "4.1";
    public const int MaxFps = 30;
    public const int MaxWidth = 1920;

    public static string[] ConversionArgs(string source, string target, bool progressPipe = true)
    {
        var args = new List<string>
        {
            "-y", "-hide_banner", "-loglevel", "error",
            "-i", source,
            "-map", "0:v:0", "-map", "0:a:0?",
            "-c:v", VideoCodec, "-preset", "medium",
            "-profile:v", Profile, "-level", Level,
            "-pix_fmt", PixelFormat,
            "-vf", $"scale='min({MaxWidth},iw)':-2:force_original_aspect_ratio=decrease",
            "-fpsmax", MaxFps.ToString(),
            "-crf", "20", "-maxrate", "8M", "-bufsize", "16M",
            "-c:a", AudioCodec, "-b:a", "192k", "-ar", "48000", "-ac", "2",
            "-movflags", "+faststart"
        };
        if (progressPipe)
            args.AddRange(new[] { "-progress", "pipe:1", "-nostats" });
        args.AddRange(new[] { "-f", "mp4", target });
        return args.ToArray();
    }
}
