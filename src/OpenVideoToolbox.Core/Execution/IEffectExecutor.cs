// v2 Effect System — Design Draft

using System.Text.Json;
using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

/// <summary>
/// Internal executor for complex effects that cannot be expressed as
/// FFmpeg filter templates. Used only when
/// <see cref="Editing.IEffectDefinition.FfmpegTemplates"/> is null.
/// Registered in a render-engine-internal dictionary keyed by effect type.
/// </summary>
public interface IEffectExecutor
{
    /// <summary>
    /// Append one or more FFmpeg filter statements for this effect, transforming
    /// the stream identified by <paramref name="inputLabel"/> into
    /// <paramref name="outputLabel"/>.
    /// </summary>
    void AppendFilterGraph(
        IReadOnlyDictionary<string, JsonElement> parameters,
        EffectRenderContext context,
        string inputLabel,
        string outputLabel,
        ICollection<string> filters);
}

/// <summary>
/// Context provided to <see cref="IEffectExecutor"/> implementations,
/// giving access to the current clip, its parent track, and the full timeline.
/// </summary>
public sealed record EffectRenderContext
{
    /// <summary>The clip to which the effect is applied.</summary>
    public required TimelineClip Clip { get; init; }

    /// <summary>The track containing the clip.</summary>
    public required TimelineTrack Track { get; init; }

    /// <summary>The full v2 timeline for cross-track lookups.</summary>
    public required EditPlanTimeline Timeline { get; init; }
}
