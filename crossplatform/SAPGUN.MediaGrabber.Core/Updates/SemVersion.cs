using System.Globalization;
using System.Text.RegularExpressions;

namespace SapgunMediaGrabber.Updates;

public sealed class SemVersion : IComparable<SemVersion>, IEquatable<SemVersion>
{
    static readonly Regex Pattern = new(
        @"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z.-]+))?(?:\+([0-9A-Za-z.-]+))?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IReadOnlyList<string> PreRelease { get; }
    public string Original { get; }

    SemVersion(int major, int minor, int patch, IReadOnlyList<string> preRelease, string original)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        Original = original;
    }

    public bool IsPrerelease => PreRelease.Count > 0;

    public static SemVersion Parse(string value)
    {
        if (!TryParse(value, out var version))
            throw new FormatException("Not a semantic version: " + value);
        return version;
    }

    public static bool TryParse(string? value, out SemVersion version)
    {
        version = null!;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        var match = Pattern.Match(trimmed);
        if (!match.Success) return false;

        var major = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minor = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var patch = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var pre = match.Groups[4].Success
            ? match.Groups[4].Value.Split('.', StringSplitOptions.RemoveEmptyEntries)
            : Array.Empty<string>();
        if (pre.Any(part => part.Length == 0)) return false;
        version = new SemVersion(major, minor, patch, pre, trimmed.TrimStart('v', 'V'));
        return true;
    }

    public int CompareTo(SemVersion? other)
    {
        if (other is null) return 1;
        var core = Major.CompareTo(other.Major);
        if (core != 0) return core;
        core = Minor.CompareTo(other.Minor);
        if (core != 0) return core;
        core = Patch.CompareTo(other.Patch);
        if (core != 0) return core;

        if (PreRelease.Count == 0 && other.PreRelease.Count == 0) return 0;
        if (PreRelease.Count == 0) return 1;
        if (other.PreRelease.Count == 0) return -1;

        var n = Math.Min(PreRelease.Count, other.PreRelease.Count);
        for (var i = 0; i < n; i++)
        {
            var cmp = CompareIdentifier(PreRelease[i], other.PreRelease[i]);
            if (cmp != 0) return cmp;
        }
        return PreRelease.Count.CompareTo(other.PreRelease.Count);
    }

    static int CompareIdentifier(string left, string right)
    {
        var leftNum = int.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out var l);
        var rightNum = int.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out var r);
        if (leftNum && rightNum) return l.CompareTo(r);
        if (leftNum) return -1;
        if (rightNum) return 1;
        return string.CompareOrdinal(left, right);
    }

    public bool Equals(SemVersion? other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is SemVersion other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, string.Join('.', PreRelease));
    public override string ToString() => Original;

    public static bool operator <(SemVersion a, SemVersion b) => a.CompareTo(b) < 0;
    public static bool operator >(SemVersion a, SemVersion b) => a.CompareTo(b) > 0;
    public static bool operator <=(SemVersion a, SemVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >=(SemVersion a, SemVersion b) => a.CompareTo(b) >= 0;
    public static bool operator ==(SemVersion? a, SemVersion? b) => a is null ? b is null : a.Equals(b);
    public static bool operator !=(SemVersion? a, SemVersion? b) => !(a == b);
}
