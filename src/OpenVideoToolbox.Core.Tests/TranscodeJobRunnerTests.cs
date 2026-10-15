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
        var root = Path.Combine(Path.GetTempPath(), $"ovt-runner-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "input.mp4");
        await File.WriteAllBytesAsync(inputPath, new byte[100]);
        var fakeRunner = new FakeProcessRunner(async request =>
        {
            await File.WriteAllBytesAsync(request.ProducedPaths[0], new byte[10]);
            return new ExecutionResult
            {
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = request.CommandPlan,
            ProducedPaths = request.ProducedPaths
            };
        });
        var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), fakeRunner);

        try
        {
            var result = await runner.RunAsync(BuildJob(inputPath, root), executablePath: "ffmpeg-custom");

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
            Assert.Single(result.ProducedPaths);
            Assert.Equal(Path.Combine(root, "output", "clip.mp4"), result.ProducedPaths[0]);
            Assert.True(File.Exists(result.ProducedPaths[0]));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_RejectsLargerOutputWithoutReplacingExistingFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ovt-larger-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "input.mp4");
        var outputPath = Path.Combine(root, "output", "clip.mp4");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllBytesAsync(inputPath, new byte[10]);
        await File.WriteAllBytesAsync(outputPath, new byte[] { 7, 8, 9 });
        var fakeRunner = new FakeProcessRunner(async request =>
        {
            await File.WriteAllBytesAsync(request.ProducedPaths[0], new byte[100]);
            return new ExecutionResult
            {
                Status = ExecutionStatus.Succeeded,
                ExitCode = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                CommandPlan = request.CommandPlan,
                ProducedPaths = request.ProducedPaths
            };
        });

        try
        {
            var result = await new TranscodeJobRunner(new FfmpegCommandBuilder(), fakeRunner)
                .RunAsync(BuildJob(inputPath, root));

            Assert.Equal(ExecutionStatus.Failed, result.Status);
            Assert.Contains("larger than input", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(new byte[] { 7, 8, 9 }, await File.ReadAllBytesAsync(outputPath));
            Assert.Empty(Directory.GetFiles(Path.Combine(root, "output"), ".*.tmp.mp4"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_AllowsLargerOutputOnlyWhenExplicitlyEnabled()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ovt-allow-larger-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "input.mp4");
        await File.WriteAllBytesAsync(inputPath, new byte[10]);
        var fakeRunner = new FakeProcessRunner(async request =>
        {
            await File.WriteAllBytesAsync(request.ProducedPaths[0], new byte[100]);
            return new ExecutionResult
            {
                Status = ExecutionStatus.Succeeded,
                ExitCode = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                CommandPlan = request.CommandPlan,
                ProducedPaths = request.ProducedPaths
            };
        });

        try
        {
            var result = await new TranscodeJobRunner(new FfmpegCommandBuilder(), fakeRunner)
                .RunAsync(BuildJob(inputPath, root) with
                {
                    Output = BuildJob(inputPath, root).Output with { AllowLargerOutput = true }
                });

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.Equal(100, new FileInfo(result.ProducedPaths.Single()).Length);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_CopiesSubtitleSidecarAfterSuccessfulTranscode()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ovt-sidecar-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "input.mp4");
        await File.WriteAllBytesAsync(inputPath, new byte[100]);
        var subtitlePath = Path.Combine(root, "source.srt");
        await File.WriteAllTextAsync(subtitlePath, "1\n00:00:00,000 --> 00:00:01,000\nhello\n");
        try
        {
            var fakeRunner = new FakeProcessRunner(async request =>
            {
                await File.WriteAllBytesAsync(request.ProducedPaths[0], new byte[10]);
                return new ExecutionResult
                {
                Status = ExecutionStatus.Succeeded,
                ExitCode = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                CommandPlan = request.CommandPlan,
                ProducedPaths = request.ProducedPaths
                };
            });
            var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), fakeRunner);
            var outputDirectory = Path.Combine(root, "output");
            Directory.CreateDirectory(outputDirectory);
            var result = await runner.RunAsync(BuildJob(inputPath, root) with
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

    private static JobDefinition BuildJob(string? inputPath = null, string? root = null)
    {
        root ??= Directory.GetCurrentDirectory();
        return new JobDefinition
        {
            Id = "job-runner-01",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Source = new JobSource
            {
                InputPath = inputPath ?? "samples/input/clip.mkv"
            },
            Output = new JobOutput
            {
                OutputDirectory = Path.Combine(root, "output"),
                FileNameStem = "clip",
                ContainerExtension = ".mp4",
                OverwriteExisting = true
            },
            Preset = BuiltInPresetCatalog.GetRequired("h264-aac-mp4")
        };
    }
}
