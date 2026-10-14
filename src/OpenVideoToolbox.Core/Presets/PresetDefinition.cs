namespace OpenVideoToolbox.Core.Presets;

public sealed record PresetDefinition
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string? Description { get; init; }

    public PresetKind Kind { get; init; } = PresetKind.Transcode;

    public VideoEncoderSettings? Video { get; init; }

    public AudioEncoderSettings? Audio { get; init; }

    public required OutputSettings Output { get; init; }

    /// <summary>Suffix used when the CLI chooses the default output name.</summary>
    public string? OutputNameSuffix { get; init; }

    public IReadOnlyDictionary<string, string> MetadataTags { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> ExtraArguments { get; init; } = [];
}

public enum PresetKind
{
    Transcode,
    Remux,
    AudioOnly
}

public sealed record VideoEncoderSettings
{
    public required string Encoder { get; init; }

    public string? Preset { get; init; }

    public int? Crf { get; init; }

    public string? PixelFormat { get; init; }

    /// <summary>Optional output sizing applied before other video filters.</summary>
    public VideoSizingSettings? Sizing { get; init; }

    public IReadOnlyList<string> ExtraArguments { get; init; } = [];
}

public sealed record VideoSizingSettings
{
    public int? Width { get; init; }

    public int? Height { get; init; }

    /// <summary>Optional canvas ratio: 9:16, 1:1, or 16:9.</summary>
    public string? Fit { get; init; }

    public string PadColor { get; init; } = "black";
}

public sealed record AudioEncoderSettings
{
    public required string Encoder { get; init; }

    public int? BitrateKbps { get; init; }

    public int? Channels { get; init; }

    public int? SampleRate { get; init; }

    public IReadOnlyList<string> ExtraArguments { get; init; } = [];
}

public sealed record OutputSettings
{
    public required string ContainerExtension { get; init; }

    public bool FastStart { get; init; }

    public bool OverwriteExisting { get; init; }
}
