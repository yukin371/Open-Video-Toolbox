using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Presets;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicTranscodeCommandPlan()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob();

        var plan = builder.Build(job);

        Assert.Equal("ffmpeg", plan.ToolName);
        Assert.Equal("output", plan.WorkingDirectory);
        Assert.Equal(
            [
                "-y",
                "-i",
                "samples/input/sample-video.mp4",
                "-c:v",
                "libx264",
                "-preset",
                "medium",
                "-crf",
                "21",
                "-pix_fmt",
                "yuv420p",
                "-c:a",
                "aac",
                "-b:a",
                "192k",
                "-ac",
                "2",
                "-ar",
                "48000",
                "-movflags",
                "+faststart",
                "-map_metadata",
                "-1",
                Path.Combine("output", "sample-video.release.mp4")
            ],
            plan.Arguments);
    }

    [Fact]
    public void Build_UsesNoOverwriteSwitch_WhenOutputDisallowsOverwrite()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob() with
        {
            Output = new JobOutput
            {
                OutputDirectory = "encoded",
                FileNameStem = "episode-01",
                ContainerExtension = "mkv",
                OverwriteExisting = false
            }
        };

        var plan = builder.Build(job, executablePath: "ffmpeg-custom");

        Assert.Equal("ffmpeg-custom", plan.ExecutablePath);
        Assert.Equal("-n", plan.Arguments[0]);
        Assert.Equal(Path.Combine("encoded", "episode-01.mkv"), plan.Arguments[^1]);
        Assert.Contains("samples/input/sample-video.mp4", plan.CommandLine);
    }

    [Fact]
    public void Build_AddsVideoDisableFlag_ForAudioOnlyPreset()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob() with
        {
            Output = new JobOutput
            {
                OutputDirectory = "output",
                FileNameStem = "sample-video.release",
                ContainerExtension = ".m4a",
                OverwriteExisting = true
            },
            Preset = new PresetDefinition
            {
                SchemaVersion = SchemaVersions.V1,
                Id = "aac-audio-only",
                DisplayName = "AAC audio only",
                Kind = PresetKind.AudioOnly,
                Audio = new AudioEncoderSettings
                {
                    Encoder = "aac",
                    BitrateKbps = 128,
                    Channels = 2,
                    SampleRate = 44100
                },
                Output = new OutputSettings
                {
                    ContainerExtension = ".m4a",
                    FastStart = false,
                    OverwriteExisting = true
                }
            }
        };

        var plan = builder.Build(job);

        Assert.Contains("-vn", plan.Arguments);
        Assert.DoesNotContain("-c:v", plan.Arguments);
        Assert.Equal(Path.Combine("output", "sample-video.release.m4a"), plan.Arguments[^1]);
    }

    [Fact]
    public void Build_DoesNotDuplicateFastStart_WhenAlreadyProvidedInExtraArguments()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob() with
        {
            Preset = BuildJob().Preset with
            {
                ExtraArguments = ["-movflags", "+faststart"]
            }
        };

        var plan = builder.Build(job);

        Assert.Equal(1, plan.Arguments.Count(argument => argument == "-movflags"));
    }

    [Fact]
    public void Build_AddsBurnInSubtitleFilter_WhenRequested()
    {
        var plan = new FfmpegCommandBuilder().Build(BuildJob() with
        {
            SubtitlePath = Path.Combine("samples", "captions", "episode.srt"),
            SubtitleMode = "burnIn"
        });

        var filterIndex = Array.IndexOf(plan.Arguments.ToArray(), "-vf");
        Assert.True(filterIndex >= 0);
        Assert.Equal("subtitles='" + Path.GetFullPath(Path.Combine("samples", "captions", "episode.srt")).Replace("\\", "\\\\").Replace(":", "\\:") + "'", plan.Arguments[filterIndex + 1]);
    }

    [Fact]
    public void Build_ComposesSizingAndSubtitleFilters()
    {
        var plan = new FfmpegCommandBuilder().Build(BuildJob() with
        {
            Preset = BuildJob().Preset with
            {
                Video = BuildJob().Preset.Video! with
                {
                    Sizing = new VideoSizingSettings { Width = 1080, Height = 1920, Fit = "9:16" }
                }
            },
            SubtitlePath = Path.Combine("samples", "captions", "episode.srt"),
            SubtitleMode = "burnIn"
        });

        var filterIndex = Array.IndexOf(plan.Arguments.ToArray(), "-vf");
        Assert.Equal("scale=1080:1920:force_original_aspect_ratio=decrease,pad=1080:1920:(ow-iw)/2:(oh-ih)/2:color=black,setsar=1,subtitles='" + Path.GetFullPath(Path.Combine("samples", "captions", "episode.srt")).Replace("\\", "\\\\").Replace(":", "\\:") + "'", plan.Arguments[filterIndex + 1]);
    }

    [Fact]
    public void Build_AddsWatermarkFilterGraphAndMapsOriginalAudio()
    {
        var plan = new FfmpegCommandBuilder().Build(BuildJob() with
        {
            Watermark = new VideoWatermarkSettings
            {
                ImagePath = Path.Combine("samples", "brand.png"),
                Position = WatermarkPosition.BottomRight,
                Margin = 32,
                Opacity = 0.65
            }
        });

        Assert.Contains("-filter_complex", plan.Arguments);
        var graph = plan.Arguments[Array.IndexOf(plan.Arguments.ToArray(), "-filter_complex") + 1];
        Assert.Contains("[1:v]format=rgba,colorchannelmixer=aa=0.65", graph, StringComparison.Ordinal);
        Assert.Contains("overlay=main_w-overlay_w-32:main_h-overlay_h-32:shortest=1[ovt_watermarked]", graph, StringComparison.Ordinal);
        Assert.Equal("[ovt_watermarked]", plan.Arguments[Array.IndexOf(plan.Arguments.ToArray(), "-map") + 1]);
        Assert.Contains("0:a?", plan.Arguments);
    }

    [Fact]
    public void Build_AddsSubtitleForceStyle()
    {
        var plan = new FfmpegCommandBuilder().Build(BuildJob() with
        {
            SubtitlePath = "captions.srt",
            SubtitleMode = "burnIn",
            SubtitleStyle = new OpenVideoToolbox.Core.Subtitles.SubtitleStyle
            {
                FontName = "Arial",
                FontSize = 28,
                PrimaryColor = "&H00FFFFFF",
                Outline = 2,
                MarginV = 40
            }
        });

        var filterIndex = Array.IndexOf(plan.Arguments.ToArray(), "-vf");
        Assert.Contains("force_style='FontName=Arial,FontSize=28,PrimaryColour=&H00FFFFFF,Outline=2,MarginV=40'", plan.Arguments[filterIndex + 1]);
    }

    [Fact]
    public void Build_UsesDedicatedWorkingDirectory_WhenConfigured()
    {
        var plan = new FfmpegCommandBuilder().Build(BuildJob() with
        {
            Output = BuildJob().Output with { WorkingDirectory = "output/.ovt-work/job-1" }
        });

        Assert.Equal("output/.ovt-work/job-1", plan.WorkingDirectory);
        Assert.StartsWith("output", plan.Arguments[^1], StringComparison.Ordinal);
    }

    private static JobDefinition BuildJob()
    {
        return new JobDefinition
        {
            SchemaVersion = SchemaVersions.V1,
            Id = "job-0002",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero),
            Source = new JobSource
            {
                InputPath = "samples/input/sample-video.mp4"
            },
            Output = new JobOutput
            {
                OutputDirectory = "output",
                FileNameStem = "sample-video.release",
                ContainerExtension = "mp4",
                OverwriteExisting = true
            },
            Preset = new PresetDefinition
            {
                SchemaVersion = SchemaVersions.V1,
                Id = "h264-aac-mp4",
                DisplayName = "H.264 + AAC / MP4",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "libx264",
                    Preset = "medium",
                    Crf = 21,
                    PixelFormat = "yuv420p"
                },
                Audio = new AudioEncoderSettings
                {
                    Encoder = "aac",
                    BitrateKbps = 192,
                    Channels = 2,
                    SampleRate = 48000
                },
                Output = new OutputSettings
                {
                    ContainerExtension = "mp4",
                    FastStart = true,
                    OverwriteExisting = true
                },
                ExtraArguments = ["-map_metadata", "-1"]
            },
            Tags = ["phase2"]
        };
    }
}
