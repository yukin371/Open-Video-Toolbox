using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ExtractFrame_RejectsMissingExecutableWithStructuredRequest()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-extract-frame-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var result = await RunCliAsync(
                "extract-frame", "input.mp4", "--at", "00:00:01,00:00:02",
                "--output-dir", outputDirectory, "--ffmpeg", "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("extract-frame", envelope["command"]!.GetValue<string>());
            Assert.Equal(2, envelope["payload"]!["extractFrame"]!["points"]!.AsArray().Count);
            Assert.Contains("missing-ffmpeg", envelope["payload"]!["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractFrame_FixedIntervalCreatesExpectedPoints()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-extract-frame-interval-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var result = await RunCliAsync(
                "extract-frame", "input.mp4",
                "--start", "00:00:02",
                "--interval", "00:00:03",
                "--count", "3",
                "--output-dir", outputDirectory,
                "--ffmpeg", "missing-ffmpeg");

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var points = envelope["payload"]!["extractFrame"]!["points"]!.AsArray();
            Assert.Equal(3, points.Count);
            Assert.Equal("00:00:02", points[0]!["at"]!.GetValue<string>());
            Assert.Equal("00:00:05", points[1]!["at"]!.GetValue<string>());
            Assert.Equal("00:00:08", points[2]!["at"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
