namespace OpenVideoToolbox.Core.Presets;

public static class BuiltInPresetCatalog
{
    private static readonly IReadOnlyDictionary<string, PresetDefinition> Presets =
        new Dictionary<string, PresetDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["h264-aac-mp4"] = new PresetDefinition
            {
                Id = "h264-aac-mp4",
                DisplayName = "H.264 + AAC / MP4",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "libx264",
                    Preset = "medium",
                    Crf = 23,
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
                    ContainerExtension = ".mp4",
                    FastStart = true,
                    OverwriteExisting = false
                },
                OutputNameSuffix = "_x264",
                ExtraArguments = ["-map_metadata", "-1"]
            },
            ["h264-aac-fast"] = new PresetDefinition
            {
                Id = "h264-aac-fast",
                DisplayName = "H.264 + AAC / MP4 (Fast)",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings { Encoder = "libx264", Preset = "veryfast", Crf = 24, PixelFormat = "yuv420p" },
                Audio = new AudioEncoderSettings { Encoder = "aac", BitrateKbps = 160, Channels = 2, SampleRate = 48000 },
                Output = new OutputSettings { ContainerExtension = ".mp4", FastStart = true, OverwriteExisting = false },
                OutputNameSuffix = "_x264_fast",
                ExtraArguments = ["-map_metadata", "-1"]
            },
            ["h264-aac-720p"] = new PresetDefinition
            {
                Id = "h264-aac-720p",
                DisplayName = "H.264 + AAC / MP4 (720p)",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings { Encoder = "libx264", Preset = "medium", Crf = 23, PixelFormat = "yuv420p", ExtraArguments = ["-vf", "scale=-2:720"] },
                Audio = new AudioEncoderSettings { Encoder = "aac", BitrateKbps = 160, Channels = 2, SampleRate = 48000 },
                Output = new OutputSettings { ContainerExtension = ".mp4", FastStart = true, OverwriteExisting = false },
                OutputNameSuffix = "_x264_720p",
                ExtraArguments = ["-map_metadata", "-1"]
            },
            ["h265-aac-mp4"] = new PresetDefinition
            {
                Id = "h265-aac-mp4",
                DisplayName = "H.265 + AAC / MP4",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "libx265",
                    Preset = "medium",
                    Crf = 26,
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
                    ContainerExtension = ".mp4",
                    FastStart = true,
                    OverwriteExisting = false
                },
                OutputNameSuffix = "_x265",
                ExtraArguments = ["-map_metadata", "-1"]
            },
            ["copy-aac-mkv"] = new PresetDefinition
            {
                Id = "copy-aac-mkv",
                DisplayName = "Copy Video + AAC / MKV",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "copy"
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
                    ContainerExtension = ".mkv",
                    FastStart = false,
                    OverwriteExisting = false
                },
                OutputNameSuffix = "_copy",
                ExtraArguments = ["-map_metadata", "-1"]
            }
        };

    public static string DefaultPresetId => "h264-aac-mp4";

    public static IReadOnlyList<PresetDefinition> GetAll()
    {
        return Presets.Values.ToArray();
    }

    public static bool TryGet(string presetId, out PresetDefinition preset)
    {
        if (Presets.TryGetValue(presetId, out var found))
        {
            preset = found;
            return true;
        }

        preset = null!;
        return false;
    }

    public static PresetDefinition GetRequired(string presetId)
    {
        if (TryGet(presetId, out var preset))
        {
            return preset;
        }

        throw new KeyNotFoundException($"Unknown preset '{presetId}'.");
    }
}

