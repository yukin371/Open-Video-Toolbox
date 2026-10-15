using System.Globalization;
using System.Text;

namespace OpenVideoToolbox.Core.Subtitles;

public sealed record SubtitleCue
{
    public required TimeSpan Start { get; init; }
    public required TimeSpan End { get; init; }
    public required string Text { get; init; }
}

public sealed record SubtitleDocument
{
    public string? Language { get; init; }
    public IReadOnlyList<SubtitleCue> Cues { get; init; } = [];
}

public static class SubtitleDocumentParser
{
    public static SubtitleDocument Parse(string content, string? format = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalizedFormat = format?.Trim().ToLowerInvariant();
        if (normalizedFormat is null)
        {
            normalizedFormat = content.TrimStart().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase) ? "vtt" : "srt";
        }

        return normalizedFormat switch
        {
            "srt" => ParseSrt(content),
            "vtt" => ParseVtt(content),
            _ => throw new ArgumentException($"Unsupported subtitle format '{format}'.", nameof(format))
        };
    }

    private static SubtitleDocument ParseSrt(string content) => ParseBlocks(content, isVtt: false);

    private static SubtitleDocument ParseVtt(string content) => ParseBlocks(content, isVtt: true);

    private static SubtitleDocument ParseBlocks(string content, bool isVtt)
    {
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var cues = new List<SubtitleCue>();
        var index = 0;

        if (isVtt && lines.Length > 0 && lines[0].TrimStart('\uFEFF').StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
        {
            index = 1;
        }

        while (index < lines.Length)
        {
            while (index < lines.Length && string.IsNullOrWhiteSpace(lines[index])) index++;
            if (index >= lines.Length) break;

            var first = lines[index].Trim();
            if (!first.Contains("-->", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var timing = first;
            index++;
            var textLines = new List<string>();
            while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]))
            {
                textLines.Add(lines[index].Trim());
                index++;
            }

            var parts = timing.Split("-->", 2, StringSplitOptions.TrimEntries);
            var endToken = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            if (!TryParseTimestamp(parts[0], out var start) || !TryParseTimestamp(endToken, out var end))
            {
                throw new FormatException($"Invalid subtitle timing '{timing}'.");
            }

            var text = string.Join("\n", textLines);
            if (end <= start || string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("Subtitle cues must have non-empty text and end after start.");
            }

            cues.Add(new SubtitleCue { Start = start, End = end, Text = text });
        }

        if (cues.Count == 0) throw new FormatException("No subtitle cues were found.");
        return new SubtitleDocument { Cues = cues };
    }

    public static bool TryParseTimestamp(string value, out TimeSpan result)
    {
        var normalized = value.Trim().Replace(",", ".", StringComparison.Ordinal);
        var parts = normalized.Split(':');
        if (parts.Length == 2) normalized = "00:" + normalized;
        return TimeSpan.TryParseExact(normalized, new[] { @"hh\:mm\:ss\.fff", @"h\:mm\:ss\.fff", @"hh\:mm\:ss\.ff", @"h\:mm\:ss\.ff", @"hh\:mm\:ss", @"h\:mm\:ss" }, CultureInfo.InvariantCulture, out result);
    }
}

public static class SubtitleDocumentRenderer
{
    public static string RenderSrt(SubtitleDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var builder = new StringBuilder();
        var cues = document.Cues.OrderBy(cue => cue.Start).ThenBy(cue => cue.End).ToArray();
        for (var i = 0; i < cues.Length; i++)
        {
            var cue = cues[i];
            builder.Append(i + 1).AppendLine();
            builder.Append(FormatTimestamp(cue.Start)).Append(" --> ").AppendLine(FormatTimestamp(cue.End));
            builder.AppendLine(cue.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n'));
            if (i < cues.Length - 1) builder.AppendLine();
        }
        return builder.ToString();
    }

    private static string FormatTimestamp(TimeSpan value) => $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00},{value.Milliseconds:000}";
}
