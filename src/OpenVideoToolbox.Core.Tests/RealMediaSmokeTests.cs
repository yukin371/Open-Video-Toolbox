using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.AudioSeparation;
using OpenVideoToolbox.Core.Speech;
using System.Buffers.Binary;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class RealMediaSmokeTests
{
    [Fact]
    public async Task ProbeAsync_ParsesRealMediaFile_WhenFfprobeAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg") || !RealMediaSmokeTestHelper.IsToolAvailable("ffprobe"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-probe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "sample.mp4");
            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));

            var service = new FfprobeMediaProbeService(new DefaultProcessRunner(), new FfprobeJsonParser());
            var result = await service.ProbeAsync(inputPath, "ffprobe", TimeSpan.FromSeconds(15));

            Assert.Equal("sample.mp4", result.FileName);
            Assert.True(result.Format.Duration >= TimeSpan.FromSeconds(1.9));
            Assert.Contains(result.Streams, stream => stream.Kind == MediaStreamKind.Video);
            Assert.Contains(result.Streams, stream => stream.Kind == MediaStreamKind.Audio);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RenderRunner_ProducesVideoAndSubtitleSidecar_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-render-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.mp4");
            var subtitlePath = Path.Combine(tempDirectory, "captions.srt");
            var outputDirectory = Path.Combine(tempDirectory, "out");
            var outputPath = Path.Combine(outputDirectory, "final.mp4");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));
            await File.WriteAllTextAsync(subtitlePath, "1\n00:00:00,000 --> 00:00:01,000\nhello world\n");
            Directory.CreateDirectory(outputDirectory);

            var request = new EditPlanRenderRequest
            {
                Plan = new EditPlan
                {
                    Source = new EditPlanSource
                    {
                        InputPath = inputPath
                    },
                    Clips =
                    [
                        new EditClip
                        {
                            Id = "clip-001",
                            InPoint = TimeSpan.Zero,
                            OutPoint = TimeSpan.FromSeconds(1.2)
                        }
                    ],
                    Subtitles = new EditSubtitlePlan
                    {
                        Path = subtitlePath,
                        Mode = SubtitleMode.Sidecar
                    },
                    Output = new EditOutputPlan
                    {
                        Path = outputPath,
                        Container = "mp4"
                    }
                },
                OverwriteExisting = true
            };

            var runner = new EditPlanRenderRunner(new FfmpegEditPlanRenderCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(request, "ffmpeg", TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "final.srt")));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.Contains(Path.Combine(outputDirectory, "final.srt"), result.ProducedPaths);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AudioMixRunner_ProducesMixedAudio_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-mix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.mp4");
            var bgmPath = Path.Combine(tempDirectory, "bgm.wav");
            var outputPath = Path.Combine(tempDirectory, "mixed.wav");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));
            await RealMediaSmokeTestHelper.CreateSampleAudioAsync(bgmPath, TimeSpan.FromSeconds(2));

            var request = new EditPlanAudioMixRequest
            {
                Plan = new EditPlan
                {
                    Source = new EditPlanSource
                    {
                        InputPath = inputPath
                    },
                    Clips =
                    [
                        new EditClip
                        {
                            Id = "clip-001",
                            InPoint = TimeSpan.Zero,
                            OutPoint = TimeSpan.FromSeconds(1.5)
                        }
                    ],
                    AudioTracks =
                    [
                        new AudioTrackMix
                        {
                            Id = "bgm-01",
                            Role = AudioTrackRole.Bgm,
                            Path = bgmPath,
                            Start = TimeSpan.Zero,
                            GainDb = -12
                        }
                    ],
                    Output = new EditOutputPlan
                    {
                        Path = Path.Combine(tempDirectory, "final.mp4"),
                        Container = "mp4"
                    }
                },
                OutputPath = outputPath,
                OverwriteExisting = true
            };

            var runner = new EditPlanAudioMixRunner(new FfmpegEditPlanAudioMixCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(request, "ffmpeg", TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RenderRunner_MixesBoundVoiceTrackIntoOutputAudio_WhenFfmpegAvailable()
    {
        if (!await IsFfmpegAvailableAsync())
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-render-voice-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.mp4");
            var voicePath = Path.Combine(tempDirectory, "voice.wav");
            var baselinePath = Path.Combine(tempDirectory, "baseline.mp4");
            var mixedPath = Path.Combine(tempDirectory, "with-voice.mp4");
            var baselinePcmPath = Path.Combine(tempDirectory, "baseline.pcm");
            var mixedPcmPath = Path.Combine(tempDirectory, "with-voice.pcm");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));
            await RealMediaSmokeTestHelper.CreateSampleAudioAsync(voicePath, TimeSpan.FromSeconds(2));

            await RunRenderCommandAsync(BuildRequest(inputPath, baselinePath));
            await RunRenderCommandAsync(
                BuildRequest(
                    inputPath,
                    mixedPath,
                    [new AudioTrackMix
                    {
                        Id = "voice-main",
                        Role = AudioTrackRole.Voice,
                        Path = voicePath,
                        Start = TimeSpan.Zero,
                        GainDb = 6
                    }]));
            Assert.True(File.Exists(baselinePath));
            Assert.True(File.Exists(mixedPath));

            await DecodeAudioToPcmAsync(baselinePath, baselinePcmPath);
            await DecodeAudioToPcmAsync(mixedPath, mixedPcmPath);

            var baselineRms = await ReadPcmRmsAsync(baselinePcmPath);
            var mixedRms = await ReadPcmRmsAsync(mixedPcmPath);

            Assert.True(
                mixedRms > baselineRms * 1.7,
                $"Expected the bound voice track to raise output RMS. Baseline={baselineRms:F4}, mixed={mixedRms:F4}.");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static EditPlanRenderRequest BuildRequest(
        string inputPath,
        string outputPath,
        IReadOnlyList<AudioTrackMix>? audioTracks = null)
    {
        return new EditPlanRenderRequest
        {
            Plan = new EditPlan
            {
                Source = new EditPlanSource { InputPath = inputPath },
                Clips =
                [
                    new EditClip
                    {
                        Id = "clip-001",
                        InPoint = TimeSpan.Zero,
                        OutPoint = TimeSpan.FromSeconds(1.5)
                    }
                ],
                AudioTracks = audioTracks ?? [],
                Output = new EditOutputPlan { Path = outputPath, Container = "mp4" }
            },
            OverwriteExisting = true
        };
    }

    private static async Task DecodeAudioToPcmAsync(string inputPath, string outputPath)
    {
        var command = new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = "ffmpeg",
            Arguments = ["-y", "-i", inputPath, "-map", "0:a:0", "-f", "s16le", "-ac", "1", "-ar", "48000", outputPath],
            CommandLine = $"ffmpeg -y -i \"{inputPath}\" -map 0:a:0 -f s16le -ac 1 -ar 48000 \"{outputPath}\""
        };
        await RunCommandAsync(command);
    }

    private static async Task RunRenderCommandAsync(EditPlanRenderRequest request)
    {
        var plan = new FfmpegEditPlanRenderCommandBuilder().Build(request);
        await RunCommandAsync(plan);
    }

    private static async Task<bool> IsFfmpegAvailableAsync()
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                ArgumentList = { "-version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null)
            {
                return false;
            }

            await Task.WhenAll(
                process.StandardOutput.ReadToEndAsync(),
                process.StandardError.ReadToEndAsync(),
                process.WaitForExitAsync()).WaitAsync(TimeSpan.FromSeconds(10));
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task RunCommandAsync(CommandPlan command)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = command.ExecutablePath,
            WorkingDirectory = command.WorkingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{command.ExecutablePath}'.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync()).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(process.ExitCode == 0, $"FFmpeg failed with exit code {process.ExitCode}: {stderrTask.Result}");
    }

    private static async Task<double> ReadPcmRmsAsync(string path)
    {
        var samples = await File.ReadAllBytesAsync(path);
        var firstSample = 9_600 * sizeof(short);
        var sampleCount = (samples.Length - firstSample) / sizeof(short);
        Assert.True(sampleCount > 0);

        double sumSquares = 0;
        for (var index = 0; index < sampleCount; index++)
        {
            var sample = BinaryPrimitives.ReadInt16LittleEndian(samples.AsSpan(firstSample + index * sizeof(short), sizeof(short)));
            var normalized = sample / (double)short.MaxValue;
            sumSquares += normalized * normalized;
        }

        return Math.Sqrt(sumSquares / sampleCount);
    }

    [Fact]
    public async Task MediaCutRunner_ProducesCutClip_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-cut-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.mp4");
            var outputPath = Path.Combine(tempDirectory, "clip.mp4");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(3));

            var runner = new MediaCutRunner(new FfmpegCutCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(
                new MediaCutRequest
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    Start = TimeSpan.FromSeconds(0.5),
                    End = TimeSpan.FromSeconds(1.5),
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MediaConcatRunner_ProducesMergedClip_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-concat-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "input.mp4");
            var clip1Path = Path.Combine(tempDirectory, "clip-01.mp4");
            var clip2Path = Path.Combine(tempDirectory, "clip-02.mp4");
            var listPath = Path.Combine(tempDirectory, "clips.txt");
            var outputPath = Path.Combine(tempDirectory, "merged.mp4");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(sourcePath, TimeSpan.FromSeconds(4));

            var cutRunner = new MediaCutRunner(new FfmpegCutCommandBuilder(), new DefaultProcessRunner());
            var firstCut = await cutRunner.RunAsync(
                new MediaCutRequest
                {
                    InputPath = sourcePath,
                    OutputPath = clip1Path,
                    Start = TimeSpan.FromSeconds(0),
                    End = TimeSpan.FromSeconds(1),
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));
            var secondCut = await cutRunner.RunAsync(
                new MediaCutRequest
                {
                    InputPath = sourcePath,
                    OutputPath = clip2Path,
                    Start = TimeSpan.FromSeconds(1),
                    End = TimeSpan.FromSeconds(2),
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, firstCut.Status);
            Assert.Equal(ExecutionStatus.Succeeded, secondCut.Status);

            await File.WriteAllLinesAsync(
                listPath,
                [
                    $"file '{clip1Path.Replace("'", "'\\''", StringComparison.Ordinal)}'",
                    $"file '{clip2Path.Replace("'", "'\\''", StringComparison.Ordinal)}'"
                ]);

            var concatRunner = new MediaConcatRunner(new FfmpegConcatCommandBuilder(), new DefaultProcessRunner());
            var result = await concatRunner.RunAsync(
                new MediaConcatRequest
                {
                    InputListPath = listPath,
                    OutputPath = outputPath,
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task WhisperCppTranscriptionService_ProducesTranscript_WhenConfigured()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var modelPath = RealMediaSmokeTestHelper.GetOptionalFilePathFromEnvironment("OVT_WHISPER_MODEL_PATH");
        if (modelPath is null)
        {
            return;
        }

        var whisperCliPath = RealMediaSmokeTestHelper.GetToolFromEnvironmentOrDefault("OVT_WHISPER_CLI_PATH", "whisper-cli");
        if (!RealMediaSmokeTestHelper.IsToolAvailable(whisperCliPath))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-transcribe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "speech-sample.wav");
            await RealMediaSmokeTestHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var service = new WhisperCppTranscriptionService(
                new AudioWaveformExtractRunner(new FfmpegAudioWaveformExtractCommandBuilder(), new DefaultProcessRunner()),
                new WhisperCppTranscriptionRunner(new WhisperCppCommandBuilder(), new DefaultProcessRunner()),
                new WhisperCppJsonParser());

            var transcript = await service.TranscribeAsync(
                new WhisperCppTranscriptionRequest
                {
                    InputPath = inputPath,
                    ModelPath = modelPath
                },
                ffmpegExecutablePath: "ffmpeg",
                whisperExecutablePath: whisperCliPath,
                timeout: TimeSpan.FromMinutes(2));

            Assert.NotNull(transcript);
            Assert.NotNull(transcript.Segments);
            Assert.All(transcript.Segments, segment => Assert.True(segment.End > segment.Start));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DemucsAudioSeparationService_ProducesExpectedStems_WhenConfigured()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var demucsExecutablePath = RealMediaSmokeTestHelper.GetToolFromEnvironmentOrDefault("OVT_DEMUCS_PATH", "demucs");
        if (!RealMediaSmokeTestHelper.IsToolAvailable(demucsExecutablePath))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-demucs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.wav");
            var outputDirectory = Path.Combine(tempDirectory, "stems");
            await RealMediaSmokeTestHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var service = new DemucsAudioSeparationService(
                new DemucsSeparationRunner(new DemucsCommandBuilder(), new DefaultProcessRunner()));

            var document = await service.SeparateAsync(
                new DemucsSeparationRequest
                {
                    InputPath = inputPath,
                    OutputDirectory = outputDirectory,
                    Model = "htdemucs"
                },
                executablePath: demucsExecutablePath,
                timeout: TimeSpan.FromMinutes(5));

            Assert.Equal("htdemucs", document.Model);
            Assert.True(File.Exists(document.Stems.Vocals));
            Assert.True(File.Exists(document.Stems.Accompaniment));
            Assert.True(new FileInfo(document.Stems.Vocals).Length > 0);
            Assert.True(new FileInfo(document.Stems.Accompaniment).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
