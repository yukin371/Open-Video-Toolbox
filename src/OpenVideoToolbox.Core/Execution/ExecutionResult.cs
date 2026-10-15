using OpenVideoToolbox.Core;
using System.Text.Json.Serialization;

namespace OpenVideoToolbox.Core.Execution;

public sealed record ExecutionResult
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required ExecutionStatus Status { get; init; }

    public int? ExitCode { get; init; }

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset FinishedAtUtc { get; init; }

    public TimeSpan Duration { get; init; }

    public required CommandPlan CommandPlan { get; init; }

    public IReadOnlyList<ProcessOutputLine> OutputLines { get; init; } = [];

    public string? ErrorMessage { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OutputSizeComparison? OutputSizeComparison { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ExecutionDiagnostic>? Diagnostics { get; init; }

    public IReadOnlyList<string> ProducedPaths { get; init; } = [];
}

public sealed record ExecutionDiagnostic
{
    public required string Code { get; init; }

    public required string Severity { get; init; }

    public required string Message { get; init; }

    public string? Path { get; init; }
}

public sealed record OutputSizeComparison
{
    public required string InputPath { get; init; }

    public required string OutputPath { get; init; }

    public long InputBytes { get; init; }

    public long OutputBytes { get; init; }

    public long DeltaBytes { get; init; }

    public double? OutputToInputRatio { get; init; }

    public double? ChangePercent { get; init; }

    public bool IsLarger { get; init; }
}

public enum ExecutionStatus
{
    Succeeded,
    Failed,
    Cancelled,
    TimedOut
}

public sealed record ProcessOutputLine
{
    public DateTimeOffset TimestampUtc { get; init; }

    public ProcessOutputChannel Channel { get; init; }

    public bool IsError { get; init; }

    public required string Text { get; init; }
}

public enum ProcessOutputChannel
{
    StandardOutput,
    StandardError
}
