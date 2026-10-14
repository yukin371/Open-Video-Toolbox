namespace OpenVideoToolbox.Core.Execution;

public sealed record FrameExtractRequest
{
    public required string InputPath { get; init; }

    public required IReadOnlyList<FrameExtractPoint> Points { get; init; }

    public bool OverwriteExisting { get; init; }
}

public sealed record FrameExtractPoint
{
    public TimeSpan At { get; init; }

    public required string OutputPath { get; init; }
}
