using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class XReadyEncodeTests
{
    [Fact]
    public void ConversionArgsTargetXUploadProfile()
    {
        var args = XReadyEncode.ConversionArgs("/in.mp4", "/out.mp4");
        Assert.Contains("libx264", args);
        Assert.Contains("high", args);
        Assert.Contains("4.1", args);
        Assert.Contains("yuv420p", args);
        Assert.Contains("aac", args);
        Assert.Contains("30", args);
        Assert.Contains("+faststart", args);
        Assert.Contains(args, a => a.Contains("min(1920,iw)"));
        Assert.Equal("/in.mp4", args[Array.IndexOf(args, "-i") + 1]);
        Assert.Equal("/out.mp4", args[^1]);
        Assert.Contains("-progress", args);
    }

    [Fact]
    public void ConversionArgsCanOmitProgressPipeForCi()
    {
        var args = XReadyEncode.ConversionArgs("a", "b", progressPipe: false);
        Assert.DoesNotContain("-progress", args);
        Assert.Contains("libx264", args);
        Assert.Contains("aac", args);
    }
}
