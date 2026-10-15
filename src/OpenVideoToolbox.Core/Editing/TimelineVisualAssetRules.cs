namespace OpenVideoToolbox.Core.Editing;

internal static class TimelineVisualAssetRules
{
    private static readonly HashSet<string> StillImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".bmp",
        ".tif",
        ".tiff"
    };

    public static bool IsStillImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return !string.IsNullOrWhiteSpace(extension) && StillImageExtensions.Contains(extension);
    }

    public static bool AllowsDurationOnlyVideoClip(TimelineClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        return clip.Duration is not null
            && !string.IsNullOrWhiteSpace(clip.Src)
            && IsStillImagePath(clip.Src);
    }
}
