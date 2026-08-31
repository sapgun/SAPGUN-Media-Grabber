using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class MediaUrlDropTests
{
    [Fact]
    public void AcceptsPlainHttpsUrl()
    {
        Assert.Equal("https://www.youtube.com/watch?v=abc", MediaUrlDrop.FirstHttpUrl("https://www.youtube.com/watch?v=abc"));
    }

    [Fact]
    public void PicksFirstUrlFromDroppedText()
    {
        var text = "watch this\nhttps://x.com/caro7370/status/1 extra";
        Assert.Equal("https://x.com/caro7370/status/1", MediaUrlDrop.FirstHttpUrl(text));
    }

    [Fact]
    public void RejectsNonHttp()
    {
        Assert.Null(MediaUrlDrop.FirstHttpUrl("ftp://example.com/a"));
        Assert.Null(MediaUrlDrop.FirstHttpUrl("not a url"));
        Assert.Null(MediaUrlDrop.FirstHttpUrl(""));
    }
}
