using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;
using OpenVideoToolbox.Core.Subtitles;

namespace OpenVideoToolbox.Core.Jobs;

public sealed record JobDefinition
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string Id { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public required JobSource Source { get; init; }

    public required JobOutput Output { get; init; }

    public required PresetDefinition Preset { get; init; }

    public MediaProbeResult? ProbeSnapshot { get; init; }

    public string? SubtitlePath { get; init; }

    public string SubtitleMode { get; init; } = "none";

    public SubtitleStyle? SubtitleStyle { get; init; }

    public VideoWatermarkSettings? Watermark { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record VideoWatermarkSettings
{
    public required string ImagePath { get; init; }

    public WatermarkPosition Position { get; init; } = WatermarkPosition.BottomRight;

    public int Margin { get; init; } = 24;

    public double Opacity { get; init; } = 0.8;
}

public enum WatermarkPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public sealed record JobSource
{
    public required string InputPath { get; init; }
}

public sealed record JobOutput
{
    public required string OutputDirectory { get; init; }

    public string? WorkingDirectory { get; init; }

    public required string FileNameStem { get; init; }

    public required string ContainerExtension { get; init; }

    public bool OverwriteExisting { get; init; }

    /// <summary>Allows a successful transcode to replace the final output even when it is larger than the source.</summary>
    public bool AllowLargerOutput { get; init; }
}
