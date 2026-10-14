using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;
using System.Globalization;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class FoundationCommandSupport
{
    public static JobDefinition BuildJob(string inputPath, IReadOnlyDictionary<string, string> options, MediaProbeResult? probeSnapshot)
    {
        var resolvedInputPath = Path.GetFullPath(inputPath);
        var presetId = GetOption(options, "--preset") ?? BuiltInPresetCatalog.DefaultPresetId;
        var preset = PresetCatalog.Load(GetOption(options, "--preset-dir"), GetOption(options, "--preset-file")).GetRequired(presetId);
        preset = ApplySizingOverride(preset, options);

        var outputDirectory = GetOption(options, "--output-dir")
            ?? Path.GetDirectoryName(resolvedInputPath)
            ?? Environment.CurrentDirectory;
        var resolvedOutputDirectory = Path.GetFullPath(outputDirectory);
        var outputName = GetOption(options, "--output-name");
        if (string.IsNullOrWhiteSpace(outputName))
        {
            outputName = Path.GetFileNameWithoutExtension(resolvedInputPath) + (preset.OutputNameSuffix ?? $"_{preset.Id}");
        }
        var outputExtension = preset.Output.ContainerExtension.StartsWith('.')
            ? preset.Output.ContainerExtension
            : $".{preset.Output.ContainerExtension}";
        if (outputName.EndsWith(outputExtension, StringComparison.OrdinalIgnoreCase))
        {
            outputName = outputName[..^outputExtension.Length];
        }

        var watermark = ParseWatermark(options);
        var subtitleStyle = ParseSubtitleStyle(options);
        var subtitlePath = GetOption(options, "--subtitle");
        var subtitleMode = GetOption(options, "--subtitle-mode") ?? "none";
        if (!string.IsNullOrWhiteSpace(subtitlePath)
            && subtitleMode is not ("burnIn" or "sidecar"))
        {
            throw new ArgumentException("Option '--subtitle-mode' expects burnIn or sidecar when '--subtitle' is provided.");
        }
        if (subtitleStyle is not null && !string.Equals(subtitleMode, "burnIn", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Subtitle style options require '--subtitle-mode burnIn'.");
        }

        return new JobDefinition
        {
            Id = $"job-{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Source = new JobSource
            {
                InputPath = resolvedInputPath
            },
            Output = new JobOutput
            {
                OutputDirectory = resolvedOutputDirectory,
                FileNameStem = outputName,
                ContainerExtension = preset.Output.ContainerExtension,
                OverwriteExisting = GetOption(options, "--overwrite") == "true" || preset.Output.OverwriteExisting,
                AllowLargerOutput = string.Equals(GetOption(options, "--allow-larger-output"), "true", StringComparison.OrdinalIgnoreCase)
            },
            Preset = preset,
            ProbeSnapshot = probeSnapshot,
            Watermark = watermark,
            SubtitlePath = string.IsNullOrWhiteSpace(subtitlePath) ? null : Path.GetFullPath(subtitlePath),
            SubtitleMode = string.IsNullOrWhiteSpace(subtitlePath) ? "none" : subtitleMode,
            SubtitleStyle = subtitleStyle,
            Tags = ["cli", "phase3", presetId]
        };
    }

    private static OpenVideoToolbox.Core.Subtitles.SubtitleStyle? ParseSubtitleStyle(IReadOnlyDictionary<string, string> options)
    {
        var hasStyle = new[] { "--subtitle-font", "--subtitle-size", "--subtitle-color", "--subtitle-outline-color", "--subtitle-outline", "--subtitle-margin-v" }
            .Any(option => !string.IsNullOrWhiteSpace(GetOption(options, option)));
        if (!hasStyle) return null;

        int? size = ParseOptionalInt(options, "--subtitle-size", 1, 200);
        int? outline = ParseOptionalInt(options, "--subtitle-outline", 0, 20);
        int? marginV = ParseOptionalInt(options, "--subtitle-margin-v", 0, 1000);
        return new OpenVideoToolbox.Core.Subtitles.SubtitleStyle
        {
            FontName = GetOption(options, "--subtitle-font"),
            FontSize = size,
            PrimaryColor = GetOption(options, "--subtitle-color"),
            OutlineColor = GetOption(options, "--subtitle-outline-color"),
            Outline = outline,
            MarginV = marginV
        };
    }

    private static int? ParseOptionalInt(IReadOnlyDictionary<string, string> options, string name, int minimum, int maximum)
    {
        var raw = GetOption(options, name);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!int.TryParse(raw, out var value) || value < minimum || value > maximum)
        {
            throw new ArgumentException($"Option '{name}' expects an integer between {minimum} and {maximum}.");
        }

        return value;
    }

    private static VideoWatermarkSettings? ParseWatermark(IReadOnlyDictionary<string, string> options)
    {
        var imagePath = GetOption(options, "--watermark");
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var positionValue = GetOption(options, "--watermark-position") ?? "bottom-right";
        if (!TryParseWatermarkPosition(positionValue, out var position))
        {
            throw new ArgumentException("Option '--watermark-position' expects one of: top-left, top-center, top-right, middle-left, center, middle-right, bottom-left, bottom-center, bottom-right.");
        }

        var marginValue = GetOption(options, "--watermark-margin");
        var margin = 24;
        if (!string.IsNullOrWhiteSpace(marginValue)
            && (!int.TryParse(marginValue, out margin) || margin < 0 || margin > 4096))
        {
            throw new ArgumentException("Option '--watermark-margin' expects an integer between 0 and 4096.");
        }

        var opacityValue = GetOption(options, "--watermark-opacity");
        var opacity = 0.8d;
        if (!string.IsNullOrWhiteSpace(opacityValue)
            && (!double.TryParse(opacityValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out opacity)
                || !double.IsFinite(opacity)
                || opacity < 0
                || opacity > 1))
        {
            throw new ArgumentException("Option '--watermark-opacity' expects a number between 0 and 1.");
        }

        return new VideoWatermarkSettings
        {
            ImagePath = Path.GetFullPath(imagePath),
            Position = position,
            Margin = margin,
            Opacity = opacity
        };
    }

    private static bool TryParseWatermarkPosition(string value, out WatermarkPosition position)
    {
        var normalized = value.Trim().Replace('_', '-').ToLowerInvariant();
        position = normalized switch
        {
            "top-left" => WatermarkPosition.TopLeft,
            "top-center" => WatermarkPosition.TopCenter,
            "top-right" => WatermarkPosition.TopRight,
            "middle-left" => WatermarkPosition.MiddleLeft,
            "center" => WatermarkPosition.Center,
            "middle-right" => WatermarkPosition.MiddleRight,
            "bottom-left" => WatermarkPosition.BottomLeft,
            "bottom-center" => WatermarkPosition.BottomCenter,
            "bottom-right" => WatermarkPosition.BottomRight,
            _ => default
        };

        return normalized is "top-left" or "top-center" or "top-right" or "middle-left" or "center" or "middle-right" or "bottom-left" or "bottom-center" or "bottom-right";
    }

    private static PresetDefinition ApplySizingOverride(PresetDefinition preset, IReadOnlyDictionary<string, string> options)
    {
        var resolution = GetOption(options, "--resolution");
        var fit = GetOption(options, "--fit");
        if (string.IsNullOrWhiteSpace(resolution) && string.IsNullOrWhiteSpace(fit))
        {
            return preset;
        }

        if (preset.Video is null)
        {
            throw new InvalidOperationException("Options '--resolution' and '--fit' require a video preset.");
        }

        return preset with { Video = preset.Video with { Sizing = ParseSizing(resolution, fit, preset.Video.Sizing?.Height) } };
    }

    private static VideoSizingSettings ParseSizing(string? resolution, string? fit, int? defaultHeight)
    {
        var normalizedFit = string.IsNullOrWhiteSpace(fit) ? null : fit.Trim().Replace("×", "x", StringComparison.OrdinalIgnoreCase);
        if (normalizedFit is not null && normalizedFit is not ("9:16" or "1:1" or "16:9"))
        {
            throw new ArgumentException("Option '--fit' expects one of: 9:16, 1:1, 16:9.");
        }

        int? width = null;
        int? height = null;
        if (!string.IsNullOrWhiteSpace(resolution))
        {
            var raw = resolution.Trim().ToLowerInvariant().Replace("×", "x", StringComparison.Ordinal);
            if (raw is "480p" or "720p" or "1080p")
            {
                height = int.Parse(raw[..^1], CultureInfo.InvariantCulture);
            }
            else
            {
                var parts = raw.Split('x', StringSplitOptions.TrimEntries);
                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedWidth)
                    || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedHeight)
                    || parsedWidth <= 0
                    || parsedHeight <= 0)
                {
                    throw new ArgumentException("Option '--resolution' expects 480p, 720p, 1080p, or WIDTHxHEIGHT.");
                }

                width = parsedWidth;
                height = parsedHeight;
            }
        }

        if (normalizedFit is null)
        {
            return new VideoSizingSettings { Width = width, Height = height };
        }

        height ??= defaultHeight ?? 1080;
        if (width is not null)
        {
            var ratio = normalizedFit switch
            {
                "9:16" => 9d / 16d,
                "1:1" => 1d,
                _ => 16d / 9d
            };
            if (Math.Abs((double)width.Value / height.Value - ratio) > 0.01)
            {
                throw new ArgumentException("Option '--fit' does not match the aspect ratio in '--resolution'.");
            }
        }
        else
        {
            width = normalizedFit switch
            {
                "9:16" => (int)Math.Round(height.Value * 9d / 16d),
                "1:1" => height,
                _ => (int)Math.Round(height.Value * 16d / 9d)
            };
        }

        if (width.Value % 2 != 0)
        {
            width++;
        }

        return new VideoSizingSettings { Width = width, Height = height, Fit = normalizedFit };
    }

    public static IReadOnlyList<DependencyProbeDefinition> BuildDoctorDependencyDefinitions(
        IReadOnlyDictionary<string, string> options,
        TimeSpan? timeout)
    {
        return
        [
            CreateExecutableDependency(
                id: "ffmpeg",
                required: true,
                optionValue: GetOption(options, "--ffmpeg"),
                environmentVariableName: null,
                defaultValue: "ffmpeg",
                probeArguments: ["-version"],
                timeout),
            CreateExecutableDependency(
                id: "ffprobe",
                required: true,
                optionValue: GetOption(options, "--ffprobe"),
                environmentVariableName: null,
                defaultValue: "ffprobe",
                probeArguments: ["-version"],
                timeout),
            CreateExecutableDependency(
                id: "whisper-cli",
                required: false,
                optionValue: GetOption(options, "--whisper-cli"),
                environmentVariableName: "OVT_WHISPER_CLI_PATH",
                defaultValue: "whisper-cli",
                probeArguments: ["--help"],
                timeout),
            CreateExecutableDependency(
                id: "demucs",
                required: false,
                optionValue: GetOption(options, "--demucs"),
                environmentVariableName: "OVT_DEMUCS_PATH",
                defaultValue: "demucs",
                probeArguments: ["--help"],
                timeout),
            CreateFileDependency(
                id: "whisper-model",
                required: false,
                optionValue: GetOption(options, "--whisper-model"),
                environmentVariableName: "OVT_WHISPER_MODEL_PATH")
        ];
    }

    private static DependencyProbeDefinition CreateExecutableDependency(
        string id,
        bool required,
        string? optionValue,
        string? environmentVariableName,
        string? defaultValue,
        IReadOnlyList<string> probeArguments,
        TimeSpan? timeout)
    {
        var resolution = ResolveDependencyValue(optionValue, environmentVariableName, defaultValue);
        return new DependencyProbeDefinition
        {
            Id = id,
            Kind = DependencyProbeKind.Executable,
            Required = required,
            Source = resolution.Source,
            ResolvedValue = resolution.Value,
            ProbeArguments = probeArguments,
            Timeout = timeout
        };
    }

    private static DependencyProbeDefinition CreateFileDependency(
        string id,
        bool required,
        string? optionValue,
        string? environmentVariableName)
    {
        var resolution = ResolveDependencyValue(optionValue, environmentVariableName, defaultValue: null);
        return new DependencyProbeDefinition
        {
            Id = id,
            Kind = DependencyProbeKind.File,
            Required = required,
            Source = resolution.Source,
            ResolvedValue = string.IsNullOrWhiteSpace(resolution.Value)
                ? null
                : Path.GetFullPath(resolution.Value)
        };
    }

    private static (string? Value, DependencyValueSource Source) ResolveDependencyValue(
        string? optionValue,
        string? environmentVariableName,
        string? defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(optionValue))
        {
            return (optionValue, DependencyValueSource.Option);
        }

        if (!string.IsNullOrWhiteSpace(environmentVariableName))
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return (environmentValue.Trim(), DependencyValueSource.Environment);
            }
        }

        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            return (defaultValue, DependencyValueSource.Default);
        }

        return (null, DependencyValueSource.Unset);
    }
}
