namespace OpenVideoToolbox.Core.Subtitles;

public sealed record SubtitleClipRequest
{
    public required SubtitleDocument Document { get; init; }
    public required TimeSpan Start { get; init; }
    public required TimeSpan End { get; init; }
}

public static class SubtitleWorkflow
{
    public static SubtitleDocument Clip(SubtitleClipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.End <= request.Start) throw new ArgumentException("Clip end must be after clip start.");

        var cues = request.Document.Cues
            .Where(cue => cue.End > request.Start && cue.Start < request.End)
            .Select(cue => new SubtitleCue
            {
                Start = cue.Start < request.Start ? TimeSpan.Zero : cue.Start - request.Start,
                End = cue.End > request.End ? request.End - request.Start : cue.End - request.Start,
                Text = cue.Text
            })
            .Where(cue => cue.End > cue.Start)
            .ToArray();

        return request.Document with { Cues = cues };
    }

    public static SubtitleDocument MergeBilingual(SubtitleDocument primary, SubtitleDocument secondary, bool primaryFirst = true)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);
        if (primary.Cues.Count != secondary.Cues.Count)
        {
            throw new ArgumentException("Primary and secondary subtitle files must contain the same number of cues.");
        }

        var merged = new List<SubtitleCue>(primary.Cues.Count);
        for (var i = 0; i < primary.Cues.Count; i++)
        {
            var left = primary.Cues[i];
            var right = secondary.Cues[i];
            if (Math.Abs((left.Start - right.Start).TotalMilliseconds) > 50 || Math.Abs((left.End - right.End).TotalMilliseconds) > 50)
            {
                throw new ArgumentException($"Subtitle timing mismatch at cue {i + 1}.");
            }

            merged.Add(left with { Text = primaryFirst ? $"{left.Text}\n{right.Text}" : $"{right.Text}\n{left.Text}" });
        }

        return primary with { Cues = merged };
    }
}
