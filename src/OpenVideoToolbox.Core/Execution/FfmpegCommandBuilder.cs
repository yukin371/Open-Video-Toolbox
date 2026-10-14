using System.Text;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Presets;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegCommandBuilder
{
    public CommandPlan Build(JobDefinition job, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(job);

        var arguments = new List<string>();
        var outputPath = BuildOutputPath(job.Output);

        arguments.Add(job.Output.OverwriteExisting ? "-y" : "-n");
        arguments.Add("-i");
        arguments.Add(job.Source.InputPath);

        if (job.Watermark is not null)
        {
            if (job.Preset.Video is null || string.Equals(job.Preset.Video.Encoder, "copy", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A watermark requires a video re-encode preset.", nameof(job));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(job.Watermark.ImagePath);
            if (job.Watermark.Margin < 0 || job.Watermark.Margin > 4096)
            {
                throw new ArgumentOutOfRangeException(nameof(job), "Watermark margin must be between 0 and 4096 pixels.");
            }

            if (!double.IsFinite(job.Watermark.Opacity) || job.Watermark.Opacity is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(job), "Watermark opacity must be between 0 and 1.");
            }

            var watermarkPath = Path.GetFullPath(job.Watermark.ImagePath);
            var outputPathFull = Path.GetFullPath(outputPath);
            var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (string.Equals(watermarkPath, Path.GetFullPath(job.Source.InputPath), pathComparison)
                || string.Equals(watermarkPath, outputPathFull, pathComparison))
            {
                throw new ArgumentException("The watermark image must be distinct from both input and output files.", nameof(job));
            }

            if (!Enum.IsDefined(job.Watermark.Position))
            {
                throw new ArgumentOutOfRangeException(nameof(job), "Watermark position is not supported.");
            }

            arguments.Add("-loop");
            arguments.Add("1");
            arguments.Add("-i");
            arguments.Add(watermarkPath);
        }

        var filters = new List<string>();
        ApplyPresetArguments(arguments, job.Preset, filters);
        AppendSubtitleFilter(filters, job);
        if (job.Watermark is not null)
        {
            arguments.Add("-filter_complex");
            arguments.Add(BuildWatermarkFilterGraph(filters, job.Watermark));
            arguments.Add("-map");
            arguments.Add("[ovt_watermarked]");
            arguments.Add("-map");
            arguments.Add("0:a?");
        }
        else if (filters.Count > 0)
        {
            arguments.Add("-vf");
            arguments.Add(string.Join(',', filters));
        }
        arguments.Add(outputPath);

        return new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = string.IsNullOrWhiteSpace(job.Output.WorkingDirectory)
                ? job.Output.OutputDirectory
                : job.Output.WorkingDirectory,
            Arguments = arguments,
            CommandLine = BuildCommandLine(executablePath, arguments)
        };
    }

    public static string BuildOutputPath(JobOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var extension = NormalizeExtension(output.ContainerExtension);
        return Path.Combine(output.OutputDirectory, output.FileNameStem + extension);
    }

    private static void ApplyPresetArguments(List<string> arguments, PresetDefinition preset, List<string> filters)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(preset);

        if (preset.Kind == PresetKind.AudioOnly)
        {
            arguments.Add("-vn");
        }
        else if (preset.Video is not null)
        {
            arguments.Add("-c:v");
            arguments.Add(preset.Video.Encoder);

            if (!string.IsNullOrWhiteSpace(preset.Video.Preset))
            {
                arguments.Add("-preset");
                arguments.Add(preset.Video.Preset);
            }

            if (preset.Video.Crf is not null)
            {
                arguments.Add("-crf");
                arguments.Add(preset.Video.Crf.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(preset.Video.PixelFormat))
            {
                arguments.Add("-pix_fmt");
                arguments.Add(preset.Video.PixelFormat);
            }

            if (preset.Video.Sizing is not null)
            {
                filters.Add(BuildSizingFilter(preset.Video.Sizing));
            }

            AppendExtraArguments(arguments, preset.Video.ExtraArguments, filters);
        }
        else
        {
            arguments.Add("-vn");
        }

        if (preset.Audio is not null)
        {
            arguments.Add("-c:a");
            arguments.Add(preset.Audio.Encoder);

            if (preset.Audio.BitrateKbps is not null)
            {
                arguments.Add("-b:a");
                arguments.Add($"{preset.Audio.BitrateKbps.Value}k");
            }

            if (preset.Audio.Channels is not null)
            {
                arguments.Add("-ac");
                arguments.Add(preset.Audio.Channels.Value.ToString());
            }

            if (preset.Audio.SampleRate is not null)
            {
                arguments.Add("-ar");
                arguments.Add(preset.Audio.SampleRate.Value.ToString());
            }

            AppendExtraArguments(arguments, preset.Audio.ExtraArguments, filters);
        }
        else
        {
            arguments.Add("-an");
        }

        if (preset.Output.FastStart && !HasFastStartArguments(preset.ExtraArguments))
        {
            arguments.Add("-movflags");
            arguments.Add("+faststart");
        }

        AppendExtraArguments(arguments, preset.ExtraArguments, filters);
    }

    private static void AppendSubtitleFilter(List<string> filters, JobDefinition job)
    {
        if (!string.Equals(job.SubtitleMode, "burnIn", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(job.SubtitlePath))
        {
            return;
        }

        filters.Add($"subtitles='{EscapeFilterPath(Path.GetFullPath(job.SubtitlePath))}'{BuildForceStyle(job.SubtitleStyle)}");
    }

    private static string BuildForceStyle(OpenVideoToolbox.Core.Subtitles.SubtitleStyle? style)
    {
        if (style is null)
        {
            return string.Empty;
        }

        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(style.FontName)) values.Add($"FontName={EscapeStyleValue(style.FontName)}");
        if (style.FontSize is not null) values.Add($"FontSize={ValidateRange(style.FontSize.Value, 1, 200, "FontSize")}");
        if (!string.IsNullOrWhiteSpace(style.PrimaryColor)) values.Add($"PrimaryColour={ValidateAssColor(style.PrimaryColor)}");
        if (!string.IsNullOrWhiteSpace(style.OutlineColor)) values.Add($"OutlineColour={ValidateAssColor(style.OutlineColor)}");
        if (style.Outline is not null) values.Add($"Outline={ValidateRange(style.Outline.Value, 0, 20, "Outline")}");
        if (style.MarginV is not null) values.Add($"MarginV={ValidateRange(style.MarginV.Value, 0, 1000, "MarginV")}");
        return values.Count == 0 ? string.Empty : $":force_style='{string.Join(',', values)}'";
    }

    private static string EscapeStyleValue(string value)
    {
        if (value.Any(character => !char.IsLetterOrDigit(character) && character is not ' ' and not '-' and not '_'))
        {
            throw new ArgumentException("Subtitle font name contains unsupported characters.", nameof(value));
        }

        return value;
    }

    private static string ValidateAssColor(string value)
    {
        var normalized = value.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^&H[0-9A-Fa-f]{6,8}$"))
        {
            throw new ArgumentException("Subtitle colors must use ASS format like &H00FFFFFF.", nameof(value));
        }

        return normalized;
    }

    private static int ValidateRange(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    private static string BuildWatermarkFilterGraph(
        IReadOnlyList<string> filters,
        VideoWatermarkSettings watermark)
    {
        var filterBuilder = new StringBuilder();
        filterBuilder.Append("[0:v]");
        if (filters.Count == 0)
        {
            filterBuilder.Append("null");
        }
        else
        {
            filterBuilder.Append(string.Join(',', filters));
        }

        filterBuilder.Append("[ovt_base];[1:v]format=rgba,colorchannelmixer=aa=");
        filterBuilder.Append(watermark.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        filterBuilder.Append("[ovt_mark];[ovt_base][ovt_mark]overlay=");
        filterBuilder.Append(GetWatermarkPositionExpression(watermark.Position, watermark.Margin));
        filterBuilder.Append(":shortest=1[ovt_watermarked]");
        return filterBuilder.ToString();
    }

    private static string GetWatermarkPositionExpression(WatermarkPosition position, int margin)
    {
        var vertical = position switch
        {
            WatermarkPosition.TopLeft or WatermarkPosition.TopCenter or WatermarkPosition.TopRight => margin.ToString(),
            WatermarkPosition.MiddleLeft or WatermarkPosition.Center or WatermarkPosition.MiddleRight => "(main_h-overlay_h)/2",
            _ => $"main_h-overlay_h-{margin}"
        };
        var horizontal = position switch
        {
            WatermarkPosition.TopLeft or WatermarkPosition.MiddleLeft or WatermarkPosition.BottomLeft => margin.ToString(),
            WatermarkPosition.TopCenter or WatermarkPosition.Center or WatermarkPosition.BottomCenter => "(main_w-overlay_w)/2",
            _ => $"main_w-overlay_w-{margin}"
        };

        return $"{horizontal}:{vertical}";
    }

    private static string BuildSizingFilter(VideoSizingSettings sizing)
    {
        if (sizing.Width is not > 0 && sizing.Height is not > 0)
        {
            throw new ArgumentException("Video sizing requires a positive width or height.", nameof(sizing));
        }

        if (!string.IsNullOrWhiteSpace(sizing.Fit))
        {
            if (sizing.Width is not > 0 || sizing.Height is not > 0)
            {
                throw new ArgumentException("Video sizing with fit requires both width and height.", nameof(sizing));
            }

            if (sizing.Fit is not ("9:16" or "1:1" or "16:9"))
            {
                throw new ArgumentException("Video sizing fit must be 9:16, 1:1, or 16:9.", nameof(sizing));
            }

            if (sizing.Width.Value % 2 != 0 || sizing.Height.Value % 2 != 0)
            {
                throw new ArgumentException("Video sizing width and height must be even numbers.", nameof(sizing));
            }

            if (string.IsNullOrWhiteSpace(sizing.PadColor)
                || sizing.PadColor.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '#' and not '@' and not '.'))
            {
                throw new ArgumentException("Video sizing pad color contains unsupported characters.", nameof(sizing));
            }

            return $"scale={sizing.Width}:{sizing.Height}:force_original_aspect_ratio=decrease,pad={sizing.Width}:{sizing.Height}:(ow-iw)/2:(oh-ih)/2:color={sizing.PadColor},setsar=1";
        }

        return sizing.Width is > 0 && sizing.Height is > 0
            ? $"scale={sizing.Width}:{sizing.Height}:force_original_aspect_ratio=decrease:force_divisible_by=2"
            : $"scale=-2:{sizing.Height}";
    }

    private static string EscapeFilterPath(string path) => path
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(":", "\\:", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal);

    private static void AppendExtraArguments(List<string> arguments, IReadOnlyList<string> extraArguments, List<string> filters)
    {
        for (var index = 0; index < extraArguments.Count; index++)
        {
            var argument = extraArguments[index];
            if (string.Equals(argument, "-vf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(argument, "-filter:v", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 < extraArguments.Count && !string.IsNullOrWhiteSpace(extraArguments[index + 1]))
                {
                    filters.Add(extraArguments[++index]);
                }

                continue;
            }

            if (!string.IsNullOrWhiteSpace(argument))
            {
                arguments.Add(argument);
            }
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Container extension is required.", nameof(extension));
        }

        return extension.StartsWith('.') ? extension : $".{extension}";
    }

    private static bool HasFastStartArguments(IReadOnlyList<string> extraArguments)
    {
        for (var index = 0; index < extraArguments.Count - 1; index++)
        {
            if (string.Equals(extraArguments[index], "-movflags", StringComparison.OrdinalIgnoreCase)
                && extraArguments[index + 1].Contains("+faststart", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildCommandLine(string executablePath, IReadOnlyList<string> arguments)
    {
        var builder = new StringBuilder(executablePath);

        foreach (var argument in arguments)
        {
            builder.Append(' ');
            builder.Append(Quote(argument));
        }

        return builder.ToString();
    }

    private static string Quote(string argument)
    {
        if (argument.Length == 0)
        {
            return "\"\"";
        }

        return argument.IndexOfAny([' ', '\t', '"']) >= 0
            ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : argument;
    }
}
