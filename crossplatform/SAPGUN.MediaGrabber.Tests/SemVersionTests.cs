using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class SemVersionTests
{
    [Theory]
    [InlineData("0.3.0-alpha.1", "0.3.0-alpha.2", -1)]
    [InlineData("0.3.0-alpha.2", "0.3.0-beta.1", -1)]
    [InlineData("0.3.0-beta.1", "0.3.0", -1)]
    [InlineData("0.2.2", "0.3.0-alpha.1", -1)]
    [InlineData("0.3.0", "0.3.0", 0)]
    [InlineData("v0.3.0-alpha.1", "0.3.0-alpha.1", 0)]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1", -1)]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta", -1)]
    [InlineData("1.0.0-alpha.beta", "1.0.0-beta", -1)]
    [InlineData("1.0.0-beta", "1.0.0-beta.2", -1)]
    [InlineData("1.0.0-beta.2", "1.0.0-beta.11", -1)]
    [InlineData("1.0.0-beta.11", "1.0.0-rc.1", -1)]
    [InlineData("1.0.0-rc.1", "1.0.0", -1)]
    public void ComparesPrereleaseOrdering(string left, string right, int expectedSign)
    {
        var a = SemVersion.Parse(left);
        var b = SemVersion.Parse(right);
        var cmp = a.CompareTo(b);
        Assert.Equal(expectedSign, Math.Sign(cmp));
        Assert.Equal(-expectedSign, Math.Sign(b.CompareTo(a)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("0.3")]
    [InlineData("v")]
    [InlineData("0.3.0-")]
    public void RejectsMalformedVersions(string? value)
    {
        Assert.False(SemVersion.TryParse(value, out _));
    }

    [Fact]
    public void IgnoresBuildMetadataWhenComparing()
    {
        Assert.Equal(0, SemVersion.Parse("1.0.0+aaa").CompareTo(SemVersion.Parse("1.0.0+bbb")));
    }

    [Fact]
    public void EqualityOperatorsUseSemanticComparison()
    {
        Assert.True(SemVersion.Parse("v0.2.2") == SemVersion.Parse("0.2.2"));
        Assert.False(SemVersion.Parse("0.2.2") == SemVersion.Parse("0.2.3"));
    }

    [Fact]
    public void DoesNotUseLexicographicStringOrder()
    {
        Assert.True(SemVersion.Parse("0.9.0") < SemVersion.Parse("0.10.0"));
        Assert.True(SemVersion.Parse("1.0.0-beta.2") < SemVersion.Parse("1.0.0-beta.11"));
    }
}
