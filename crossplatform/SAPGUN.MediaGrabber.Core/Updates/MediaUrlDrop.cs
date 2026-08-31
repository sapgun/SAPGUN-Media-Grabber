using System.Text.RegularExpressions;

namespace SapgunMediaGrabber.Updates;

public static class MediaUrlDrop
{
    static readonly Regex HttpUrl = new(@"https?://[^\s<>""']+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string? FirstHttpUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var trimmed = text.Trim();
        if (IsHttpUrl(trimmed)) return trimmed;

        foreach (var token in trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = token.Trim().TrimEnd('.', ',', ';', ')');
            if (IsHttpUrl(candidate)) return candidate;
        }

        var match = HttpUrl.Match(trimmed);
        return match.Success ? match.Value.TrimEnd('.', ',', ';', ')') : null;
    }

    static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
