using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace SapgunMediaGrabber;

internal static class ClipboardCompatExtensions
{
    public static Task<string?> GetTextAsync(this IClipboard clipboard)
        => ClipboardExtensions.TryGetTextAsync(clipboard);
}
