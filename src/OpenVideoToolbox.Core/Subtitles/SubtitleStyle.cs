namespace OpenVideoToolbox.Core.Subtitles;

public sealed record SubtitleStyle
{
    public string? FontName { get; init; }

    public int? FontSize { get; init; }

    public string? PrimaryColor { get; init; }

    public string? OutlineColor { get; init; }

    public int? Outline { get; init; }

    public int? MarginV { get; init; }
}
