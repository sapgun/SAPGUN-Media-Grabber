using SapgunMediaGrabber.Updates;
using Xunit;

namespace SapgunMediaGrabber.Tests;

public class BundledLicenseLocatorTests
{
    [Fact]
    public void FindsNoticesNextToTheApp()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sapgun-lic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "THIRD_PARTY_NOTICES.txt");
            File.WriteAllText(path, "notices");
            Assert.Equal(path, BundledLicenseLocator.FindNotices(dir));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void FindsNoticesInMacOsResources()
    {
        var root = Path.Combine(Path.GetTempPath(), "sapgun-app-" + Guid.NewGuid().ToString("N"));
        var macos = Path.Combine(root, "Contents", "MacOS");
        var resources = Path.Combine(root, "Contents", "Resources");
        Directory.CreateDirectory(macos);
        Directory.CreateDirectory(resources);
        try
        {
            var path = Path.Combine(resources, "THIRD_PARTY_NOTICES.txt");
            File.WriteAllText(path, "notices");
            Assert.Equal(path, BundledLicenseLocator.FindNotices(macos));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void ReturnsNullWhenMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sapgun-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.Null(BundledLicenseLocator.FindNotices(dir));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
