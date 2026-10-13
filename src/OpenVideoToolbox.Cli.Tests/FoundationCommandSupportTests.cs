using OpenVideoToolbox.Cli;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed class FoundationCommandSupportTests
{
    [Fact]
    public void BuildJob_ResolvesInputAndOutputDirectoryToAbsolutePaths()
    {
        var job = FoundationCommandSupport.BuildJob(
            Path.Combine("fixtures", "input.mp4"),
            new Dictionary<string, string>
            {
                ["--output-dir"] = Path.Combine("artifacts", "run")
            },
            probeSnapshot: null);

        Assert.Equal(Path.GetFullPath(Path.Combine("fixtures", "input.mp4")), job.Source.InputPath);
        Assert.Equal(Path.GetFullPath(Path.Combine("artifacts", "run")), job.Output.OutputDirectory);
    }

    [Fact]
    public void BuildJob_AcceptsOutputNameWithPresetExtensionWithoutDuplicatingIt()
    {
        var job = FoundationCommandSupport.BuildJob(
            "input.mp4",
            new Dictionary<string, string>
            {
                ["--output-dir"] = "run-output",
                ["--output-name"] = "run.mp4"
            },
            probeSnapshot: null);

        Assert.Equal("run", job.Output.FileNameStem);
        Assert.Equal(".mp4", job.Output.ContainerExtension);
    }

    [Fact]
    public void BuildJob_DefaultsToSourceDirectoryAndPresetSuffix()
    {
        var input = Path.Combine("fixtures", "video01.mp4");
        var job = FoundationCommandSupport.BuildJob(input, new Dictionary<string, string>(), probeSnapshot: null);

        Assert.Equal(Path.GetFullPath("fixtures"), job.Output.OutputDirectory);
        Assert.Equal("video01_x264", job.Output.FileNameStem);
    }

    [Fact]
    public void BuildJob_LoadsExplicitCustomPresetFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ovt-preset-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var presetPath = Path.Combine(directory, "custom.json");
            File.WriteAllText(presetPath, """
                {
                  "schemaVersion": 1,
                  "id": "custom-h264",
                  "displayName": "Custom H264",
                  "kind": "transcode",
                  "video": { "encoder": "libx264", "preset": "fast", "crf": 28, "pixelFormat": "yuv420p" },
                  "audio": { "encoder": "aac", "bitrateKbps": 96, "channels": 2, "sampleRate": 48000 },
                  "output": { "containerExtension": ".mp4", "fastStart": true, "overwriteExisting": false },
                  "outputNameSuffix": "_small"
                }
                """);

            var job = FoundationCommandSupport.BuildJob(
                "input.mp4",
                new Dictionary<string, string>
                {
                    ["--preset"] = "custom-h264",
                    ["--preset-file"] = presetPath
                },
                probeSnapshot: null);

            Assert.Equal("custom-h264", job.Preset.Id);
            Assert.Equal("input_small", job.Output.FileNameStem);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
