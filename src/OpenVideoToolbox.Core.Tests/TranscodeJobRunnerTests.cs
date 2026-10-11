using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Presets;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class TranscodeJobRunnerTests
{
    [Fact]
    public async Task RunAsync_BuildsCommandAndPassesProducedPath()
    {
        var fakeRunner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
        {
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = request.CommandPlan,
            ProducedPaths = request.ProducedPaths
        }));
        var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), fakeRunner);

        var result = await runner.RunAsync(BuildJob(), executablePath: "ffmpeg-custom");

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Single(fakeRunner.LastRequest.ProducedPaths);
        Assert.Equal(Path.Combine("output", "clip.mp4"), fakeRunner.LastRequest.ProducedPaths[0]);
    }

    [Fact]
    public async Task RunAsync_CopiesSubtitleSidecarAfterSuccessfulTranscode()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ovt-sidecar-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var subtitlePath = Path.Combine(root, "source.srt");
        await File.WriteAllTextAsync(subtitlePath, "1\n00:00:00,000 --> 00:00:01,000\nhello\n");
        try
        {
            var fakeRunner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
            {
                Status = ExecutionStatus.Succeeded,
                ExitCode = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                CommandPlan = request.CommandPlan,
                ProducedPaths = request.ProducedPaths
            }));
            var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), fakeRunner);
            var outputDirectory = Path.Combine(root, "output");
            Directory.CreateDirectory(outputDirectory);
            var result = await runner.RunAsync(BuildJob() with
            {
                SubtitlePath = subtitlePath,
                SubtitleMode = "sidecar",
                Output = new JobOutput
                {
                    OutputDirectory = outputDirectory,
                    FileNameStem = "clip",
                    ContainerExtension = ".mp4",
                    OverwriteExisting = true
                }
            });

            var sidecarPath = Path.Combine(outputDirectory, "clip.srt");
            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.Contains(sidecarPath, result.ProducedPaths);
            Assert.True(File.Exists(sidecarPath));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static JobDefinition BuildJob()
    {
        return new JobDefinition
        {
            Id = "job-runner-01",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Source = new JobSource
            {
                InputPath = "samples/input/clip.mkv"
            },
            Output = new JobOutput
            {
                OutputDirectory = "output",
                FileNameStem = "clip",
                ContainerExtension = ".mp4",
                OverwriteExisting = true
            },
            Preset = BuiltInPresetCatalog.GetRequired("h264-aac-mp4")
        };
    }
}
