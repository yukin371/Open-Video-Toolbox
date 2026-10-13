using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed class CompressionCommandsIntegrationTests
{
    [Fact]
    public async Task Run_DefaultOutputAndSizeComparisonMatchRealFiles()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg") || !CliTestProcessHelper.IsToolAvailable("ffprobe"))
        {
            return;
        }

        var directory = CreateTempDirectory();
        try
        {
            var input = Path.Combine(directory, "video01.mp4");
            var output = Path.Combine(directory, "video01_x264.mp4");
            var report = Path.Combine(directory, "run.json");
            await CliTestProcessHelper.CreateSampleVideoAsync(input, TimeSpan.FromSeconds(1));
            var inputBytes = new FileInfo(input).Length;

            var result = await CliTestProcessHelper.RunCliAsync("run", input, "--json-out", report, "--timeout-seconds", "30");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(output));
            Assert.Equal(inputBytes, new FileInfo(input).Length);
            var envelope = JsonNode.Parse(result.StdOut)!;
            Assert.True(JsonNode.DeepEquals(envelope, JsonNode.Parse(await File.ReadAllTextAsync(report))));
            var comparison = envelope["payload"]!["sizeComparison"]!;
            var outputBytes = new FileInfo(output).Length;
            Assert.Equal(input, comparison["inputPath"]!.GetValue<string>());
            Assert.Equal(output, comparison["outputPath"]!.GetValue<string>());
            Assert.Equal(inputBytes, comparison["inputBytes"]!.GetValue<long>());
            Assert.Equal(outputBytes, comparison["outputBytes"]!.GetValue<long>());
            Assert.Equal(outputBytes - inputBytes, comparison["deltaBytes"]!.GetValue<long>());
            Assert.Equal(Math.Round((double)outputBytes / inputBytes, 4), comparison["outputToInputRatio"]!.GetValue<double>());
            Assert.Equal(Math.Round((double)(outputBytes - inputBytes) / inputBytes * 100, 2), comparison["changePercent"]!.GetValue<double>());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Plan_CustomPresetAndExplicitOutputOverridesReachCommandPlan()
    {
        var directory = CreateTempDirectory();
        try
        {
            var preset = Path.Combine(directory, "custom.json");
            File.WriteAllText(preset, """
                { "id": "custom-h264", "displayName": "Custom", "video": { "encoder": "libx264", "crf": 28 }, "output": { "containerExtension": ".mp4" }, "outputNameSuffix": "_custom" }
                """);
            var input = Path.Combine(directory, "video01.mp4");
            var outputDirectory = Path.Combine(directory, "exports");
            var result = await CliTestProcessHelper.RunCliAsync("plan", input, "--preset", "custom-h264", "--preset-file", preset,
                "--output-dir", outputDirectory, "--output-name", "renamed.mp4");

            Assert.Equal(0, result.ExitCode);
            var payload = JsonNode.Parse(result.StdOut)!["payload"]!;
            Assert.Equal("custom-h264", payload["job"]!["preset"]!["id"]!.GetValue<string>());
            Assert.Equal(outputDirectory, payload["job"]!["output"]!["outputDirectory"]!.GetValue<string>());
            Assert.Equal("renamed", payload["job"]!["output"]!["fileNameStem"]!.GetValue<string>());
            Assert.Equal(Path.Combine(outputDirectory, "renamed.mp4"), payload["commandPlan"]!["arguments"]!.AsArray().Last()!.GetValue<string>());
            Assert.False(Directory.Exists(outputDirectory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("presets")]
    [InlineData("plan")]
    public async Task InvalidPreset_ReturnsFailureEnvelopeAndJsonArtifact(string command)
    {
        var directory = CreateTempDirectory();
        try
        {
            var preset = Path.Combine(directory, "invalid.json");
            var report = Path.Combine(directory, "failure.json");
            File.WriteAllText(preset, "[null]");
            var arguments = new List<string> { command };
            if (command == "plan")
            {
                arguments.Add(Path.Combine(directory, "input.mp4"));
            }
            arguments.AddRange(["--preset-file", preset, "--json-out", report]);

            var result = await CliTestProcessHelper.RunCliAsync(arguments.ToArray());

            Assert.NotEqual(0, result.ExitCode);
            var envelope = JsonNode.Parse(result.StdOut)!;
            Assert.Equal(command, envelope["command"]!.GetValue<string>());
            Assert.Contains(preset, envelope["payload"]!["error"]!["message"]!.GetValue<string>());
            Assert.Contains(preset, result.StdErr);
            Assert.True(JsonNode.DeepEquals(envelope, JsonNode.Parse(await File.ReadAllTextAsync(report))));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ovt-compression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
