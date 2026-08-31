namespace SapgunMediaGrabber.Updates;

public static class BundledLicenseLocator
{
    public const string NoticesFileName = "THIRD_PARTY_NOTICES.txt";

    public static IReadOnlyList<string> CandidateNoticePaths(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        var notices = new List<string>
        {
            Path.GetFullPath(Path.Combine(baseDirectory, NoticesFileName))
        };

        var resources = Path.GetFullPath(Path.Combine(baseDirectory, "..", "Resources", NoticesFileName));
        if (!notices.Contains(resources, StringComparer.Ordinal))
            notices.Add(resources);

        return notices;
    }

    public static string? FindNotices(string baseDirectory) =>
        CandidateNoticePaths(baseDirectory).FirstOrDefault(File.Exists);
}
