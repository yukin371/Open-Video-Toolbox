using OpenVideoToolbox.Core.Presets;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class BuiltInPresetCatalogTests
{
    [Fact]
    public void GetRequired_ReturnsKnownPreset()
    {
        var preset = BuiltInPresetCatalog.GetRequired("h264-aac-mp4");

        Assert.Equal("H.264 + AAC / MP4", preset.DisplayName);
        Assert.Equal(".mp4", preset.Output.ContainerExtension);
    }

    [Fact]
    public void GetAll_ReturnsMultipleBuiltInPresets()
    {
        var presets = BuiltInPresetCatalog.GetAll();

        Assert.True(presets.Count >= 5);
        Assert.Contains(presets, preset => preset.Id == "copy-aac-mkv");
    }

    [Fact]
    public void BuiltInPresets_ExposeClearCompressionChoices()
    {
        var presets = BuiltInPresetCatalog.GetAll();

        Assert.Contains(presets, preset => preset.Id == "h264-aac-mp4" && preset.Video!.Crf == 23);
        Assert.Contains(presets, preset => preset.Id == "h264-aac-fast" && preset.Video!.Preset == "veryfast");
        Assert.Contains(presets, preset => preset.Id == "h264-aac-720p" && preset.ExtraArguments.Contains("scale=-2:720"));
        Assert.All(presets, preset => Assert.False(string.IsNullOrWhiteSpace(preset.Description)));
    }
}

