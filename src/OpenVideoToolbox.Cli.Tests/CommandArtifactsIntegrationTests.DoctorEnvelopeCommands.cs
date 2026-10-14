using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Doctor_ReturnsStructuredDependencyEnvelopeAndNonZeroWhenRequiredDependenciesMissing()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var modelPath = Path.Combine(outputDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await RunCliAsync(
                "doctor",
                "--ffmpeg",
                "missing-ffmpeg",
                "--ffprobe",
                "missing-ffprobe",
                "--whisper-cli",
                "missing-whisper",
                "--demucs",
                "missing-demucs",
                "--whisper-model",
                modelPath);

            Assert.Equal(1, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("doctor", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.False(payload["isHealthy"]!.GetValue<bool>());
            Assert.NotNull(payload["hardwareEncoders"]);
            Assert.False(payload["hardwareEncoders"]!["isAvailable"]!.GetValue<bool>());
            Assert.Equal(2, payload["missingRequiredCount"]!.GetValue<int>());
            Assert.Equal(2, payload["missingOptionalCount"]!.GetValue<int>());

            var dependencies = payload["dependencies"]!.AsArray();
            Assert.Equal(5, dependencies.Count);
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "ffmpeg"
                    && node["source"]!.GetValue<string>() == "option"
                    && !node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-model"
                    && node["kind"]!.GetValue<string>() == "file"
                    && node["isAvailable"]!.GetValue<bool>());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Doctor_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var jsonOutPath = Path.Combine(outputDirectory, "doctor.json");
        var modelPath = Path.Combine(outputDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await RunCliAsync(
                "doctor",
                "--ffmpeg",
                "missing-ffmpeg",
                "--ffprobe",
                "missing-ffprobe",
                "--whisper-cli",
                "missing-whisper",
                "--demucs",
                "missing-demucs",
                "--whisper-model",
                modelPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(1, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Doctor_DiscoversAndExplicitlyTrialsHardwareEncoder()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-hw-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-hardware",
            """
            @echo off
            echo %* | findstr /c:"-encoders" >nul
            if not errorlevel 1 (
              echo  V..... h264_nvenc NVIDIA NVENC H.264 encoder
              exit /b 0
            )
            echo ffmpeg version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            case "$*" in
              *-encoders*) printf ' V..... h264_nvenc NVIDIA NVENC H.264 encoder\n' ;;
              *) printf 'ffmpeg version n-test\n' ;;
            esac
            """);
        var ffprobePath = WriteExecutableScript(
            outputDirectory,
            "fake-ffprobe-hardware",
            "@echo off`necho ffprobe version n-test`nexit /b 0",
            "#!/usr/bin/env bash`nprintf 'ffprobe version n-test\\n'");

        try
        {
            var result = await RunCliAsync(
                "doctor",
                "--ffmpeg", ffmpegPath,
                "--ffprobe", ffprobePath,
                "--test-hardware-encoder", "h264_nvenc");

            Assert.Equal(0, result.ExitCode);
            var payload = JsonNode.Parse(result.StdOut)!["payload"]!;
            Assert.Contains(payload["hardwareEncoders"]!["encoders"]!.AsArray(), node => node!.GetValue<string>() == "h264_nvenc");
            Assert.True(payload["hardwareTrial"]!["isAvailable"]!.GetValue<bool>());
            Assert.Equal("h264_nvenc", payload["hardwareTrial"]!["encoder"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
