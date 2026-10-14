using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using System.Globalization;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class MediaCommandHandlers
{
    public static async Task<int> RunCutAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredTimeSpanOption(options, "--from", out var start, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredTimeSpanOption(options, "--to", out var end, out error))
        {
            return fail(error!);
        }

        if (end <= start)
        {
            return fail("Option '--to' must be greater than '--from'.");
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var request = new MediaCutRequest
        {
            InputPath = inputPath!,
            OutputPath = outputPath!,
            Start = start,
            End = end,
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };

        var processRunner = new DefaultProcessRunner();
        var runner = new MediaCutRunner(new FfmpegCutCommandBuilder(), processRunner);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

            var result = await runner.RunAsync(request, ffmpegPath, timeout);
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "cut",
                    preview: false,
                    BuildFailedCommandPayload("cut", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "cut",
                preview: false,
                new
                {
                    cut = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "cut",
                preview: false,
                BuildFailedCommandPayload("cut", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunConcatAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--input-list", out var inputListPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var request = new MediaConcatRequest
        {
            InputListPath = inputListPath!,
            OutputPath = outputPath!,
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };

        var processRunner = new DefaultProcessRunner();
        var runner = new MediaConcatRunner(new FfmpegConcatCommandBuilder(), processRunner);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

            var result = await runner.RunAsync(request, ffmpegPath, timeout);
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "concat",
                    preview: false,
                    BuildFailedCommandPayload("concat", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "concat",
                preview: false,
                new
                {
                    concat = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "concat",
                preview: false,
                BuildFailedCommandPayload("concat", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunExtractAudioAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredIntOption(options, "--track", out var trackIndex, out error))
        {
            return fail(error!);
        }

        if (trackIndex < 0)
        {
            return fail("Option '--track' must be zero or greater.");
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var request = new AudioExtractRequest
        {
            InputPath = inputPath!,
            OutputPath = outputPath!,
            TrackIndex = trackIndex,
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };

        var processRunner = new DefaultProcessRunner();
        var runner = new AudioExtractRunner(new FfmpegAudioExtractCommandBuilder(), processRunner);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

            var result = await runner.RunAsync(request, ffmpegPath, timeout);
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "extract-audio",
                    preview: false,
                    BuildFailedCommandPayload("extractAudio", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "extract-audio",
                preview: false,
                new
                {
                    extractAudio = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "extract-audio",
                preview: false,
                BuildFailedCommandPayload("extractAudio", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunExtractFrameAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--count", out var count, out error))
        {
            return fail(error!);
        }

        if (!TryParseFramePoints(options, count, out var times, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var outputPaths = BuildFrameOutputPaths(inputPath!, options, times.Count, out error);
        if (outputPaths is null)
        {
            return fail(error!);
        }

        var request = new FrameExtractRequest
        {
            InputPath = Path.GetFullPath(inputPath!),
            Points = times.Select((time, index) => new FrameExtractPoint
            {
                At = time,
                OutputPath = outputPaths[index]
            }).ToArray(),
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };
        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var runner = new FrameExtractRunner(new FfmpegFrameExtractCommandBuilder(), new DefaultProcessRunner());

        try
        {
            foreach (var outputPath in outputPaths)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            }

            var result = await runner.RunAsync(request, ffmpegPath, timeout);
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "extract-frame",
                    preview: false,
                    BuildFailedCommandPayload("extractFrame", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "extract-frame",
                preview: false,
                new
                {
                    extractFrame = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "extract-frame",
                preview: false,
                BuildFailedCommandPayload("extractFrame", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    private static bool TryParseFramePoints(
        IReadOnlyDictionary<string, string> options,
        int? count,
        out IReadOnlyList<TimeSpan> points,
        out string? error)
    {
        points = [];
        error = null;
        var at = GetOption(options, "--at");
        var interval = GetOption(options, "--interval");
        var start = GetOption(options, "--start");

        if (!string.IsNullOrWhiteSpace(at))
        {
            if (!string.IsNullOrWhiteSpace(interval) || count is not null || !string.IsNullOrWhiteSpace(start))
            {
                error = "Use either '--at' or '--start' with '--interval' and '--count'.";
                return false;
            }

            var parsed = new List<TimeSpan>();
            foreach (var raw in at.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var value) || value < TimeSpan.Zero)
                {
                    error = "Option '--at' expects one or more timestamps like 00:00:12.500.";
                    return false;
                }

                parsed.Add(value);
            }

            if (parsed.Count == 0)
            {
                error = "Option '--at' requires at least one timestamp.";
                return false;
            }

            points = parsed;
            return true;
        }

        if (string.IsNullOrWhiteSpace(interval) || count is null || string.IsNullOrWhiteSpace(start))
        {
            error = "Provide '--at' or provide '--start', '--interval', and '--count'.";
            return false;
        }

        if (count <= 0 || count > 10000)
        {
            error = "Option '--count' must be between 1 and 10000.";
            return false;
        }

        if (!TimeSpan.TryParse(interval, CultureInfo.InvariantCulture, out var intervalValue) || intervalValue <= TimeSpan.Zero)
        {
            error = "Option '--interval' expects a positive time span like 00:00:05.";
            return false;
        }

        if (!TimeSpan.TryParse(start, CultureInfo.InvariantCulture, out var startValue) || startValue < TimeSpan.Zero)
        {
            error = "Option '--start' expects a zero or greater timestamp.";
            return false;
        }

        var generated = new List<TimeSpan>(count.Value);
        try
        {
            for (var index = 0; index < count.Value; index++)
            {
                generated.Add(startValue + TimeSpan.FromTicks(checked(intervalValue.Ticks * index)));
            }
        }
        catch (OverflowException)
        {
            error = "The requested timestamp range exceeds the supported time span.";
            return false;
        }

        points = generated;
        return true;
    }

    private static IReadOnlyList<string>? BuildFrameOutputPaths(
        string inputPath,
        IReadOnlyDictionary<string, string> options,
        int count,
        out string? error)
    {
        error = null;
        var output = GetOption(options, "--output");
        var outputDirectory = GetOption(options, "--output-dir");
        var prefix = GetOption(options, "--prefix") ?? Path.GetFileNameWithoutExtension(inputPath);
        if (string.IsNullOrWhiteSpace(prefix)
            || prefix.Any(character => char.IsControl(character) || "<>:\"/\\|?*".Contains(character)))
        {
            error = "Option '--prefix' must be a file name without path separators or reserved characters.";
            return null;
        }
        if (!string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            error = "Use '--output' or '--output-dir', not both.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Path.GetDirectoryName(Path.GetFullPath(inputPath));
        }

        if (!string.IsNullOrWhiteSpace(output) && count == 1)
        {
            return [Path.GetFullPath(output)];
        }

        if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Path.GetDirectoryName(Path.GetFullPath(output));
        }

        var directory = Path.GetFullPath(outputDirectory!);
        var extension = !string.IsNullOrWhiteSpace(output)
            ? Path.GetExtension(output)
            : ".png";
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var outputStem = !string.IsNullOrWhiteSpace(output)
            ? Path.GetFileNameWithoutExtension(output)
            : prefix;
        return Enumerable.Range(1, count)
            .Select(index => Path.Combine(directory, $"{outputStem}_frame-{index:000}{extension}"))
            .ToArray();
    }
}
