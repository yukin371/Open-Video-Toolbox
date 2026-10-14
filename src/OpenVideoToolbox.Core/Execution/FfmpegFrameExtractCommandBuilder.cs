using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegFrameExtractCommandBuilder
{
    public CommandPlan Build(FrameExtractRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);
        if (request.Points is null || request.Points.Count == 0)
        {
            throw new ArgumentException("At least one frame point is required.", nameof(request));
        }

        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var inputPath = Path.GetFullPath(request.InputPath);
        var normalizedOutputPaths = request.Points.Select(point => Path.GetFullPath(point.OutputPath)).ToArray();
        if (normalizedOutputPaths.Any(path => pathComparer.Equals(path, inputPath)))
        {
            throw new ArgumentException("Frame output paths must not overwrite the input file.", nameof(request));
        }

        if (normalizedOutputPaths.Distinct(pathComparer).Count() != normalizedOutputPaths.Length)
        {
            throw new ArgumentException("Frame output paths must be unique.", nameof(request));
        }

        var arguments = new List<string> { request.OverwriteExisting ? "-y" : "-n" };
        foreach (var point in request.Points)
        {
            if (point.At < TimeSpan.Zero)
            {
                throw new ArgumentException("Frame timestamps must be zero or greater.", nameof(request));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(point.OutputPath);
            arguments.Add("-ss");
            arguments.Add(FormatTimestamp(point.At));
            arguments.Add("-i");
            arguments.Add(request.InputPath);
            arguments.Add("-frames:v");
            arguments.Add("1");
            arguments.Add("-an");
            arguments.Add(point.OutputPath);
        }

        return new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.Points[0].OutputPath)),
            Arguments = arguments,
            CommandLine = BuildCommandLine(executablePath, arguments)
        };
    }

    private static string FormatTimestamp(TimeSpan value)
    {
        var totalHours = (int)Math.Floor(value.TotalHours);
        return $"{totalHours:00}:{value.Minutes:00}:{value.Seconds:00}.{value.Milliseconds:000}";
    }

    private static string BuildCommandLine(string executablePath, IReadOnlyList<string> arguments)
    {
        var builder = new StringBuilder(executablePath);
        foreach (var argument in arguments)
        {
            builder.Append(' ');
            builder.Append(argument.IndexOfAny([' ', '\t', '"']) >= 0
                ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
                : argument);
        }

        return builder.ToString();
    }
}
