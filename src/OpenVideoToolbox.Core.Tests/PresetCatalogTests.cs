using OpenVideoToolbox.Core.Presets;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class PresetCatalogTests
{
    [Fact]
    public void Load_ReadsPresetPackFromExplicitDirectory()
    {
        WithPresetFile("""
            [
              { "id": "custom-a", "displayName": "A", "output": { "containerExtension": ".mp4" } },
              { "id": "custom-b", "displayName": "B", "output": { "containerExtension": "mkv" } }
            ]
            """, path =>
        {
            var catalog = PresetCatalog.Load(presetDirectory: Path.GetDirectoryName(path));

            Assert.Equal("A", catalog.GetRequired("CUSTOM-A").DisplayName);
            Assert.Equal("B", catalog.GetRequired("custom-b").DisplayName);
            Assert.Contains(catalog.GetAll(), preset => preset.Id == BuiltInPresetCatalog.DefaultPresetId);
        });
    }

    [Fact]
    public void Load_RejectsCaseInsensitiveBuiltInIdCollision()
    {
        WithPresetFile("""
            { "id": "H264-AAC-MP4", "displayName": "Override", "output": { "containerExtension": ".mp4" } }
            """, path =>
        {
            var exception = Assert.Throws<InvalidDataException>(() => PresetCatalog.Load(presetFile: path));

            Assert.Contains("Duplicate preset id", exception.Message);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("{}")]
    [InlineData("{\"id\":\"\",\"displayName\":\"Custom\",\"output\":{\"containerExtension\":\".mp4\"}}")]
    [InlineData("{\"id\":\"custom\",\"displayName\":\"Custom\",\"output\":null}")]
    public void Load_RejectsInvalidJsonOrRequiredFieldsWithFileContext(string json)
    {
        WithPresetFile(json, path =>
        {
            var exception = Assert.Throws<InvalidDataException>(() => PresetCatalog.Load(presetFile: path));

            Assert.Contains(path, exception.Message);
        });
    }

    [Theory]
    [InlineData("\"schemaVersion\":2", "schemaVersion")]
    [InlineData("\"kind\":99", "kind")]
    [InlineData("\"outputNameSuffix\":\"../other\"", "outputNameSuffix")]
    [InlineData("\"outputNameSuffix\":\"\"", "outputNameSuffix")]
    [InlineData("\"outputNameSuffix\":\"_bad:name\"", "outputNameSuffix")]
    [InlineData("\"video\":{\"encoder\":\"\"}", "encoder")]
    [InlineData("\"audio\":{\"encoder\":null}", "encoder")]
    public void Load_RejectsInvalidPresetValues(string property, string expectedMessage)
    {
        WithPresetFile($$"""
            { "id": "custom", "displayName": "Custom", "output": { "containerExtension": ".mp4" }, {{property}} }
            """, path =>
        {
            var exception = Assert.Throws<InvalidDataException>(() => PresetCatalog.Load(presetFile: path));

            Assert.Contains(path, exception.Message);
            Assert.Contains(expectedMessage, exception.Message);
        });
    }

    [Theory]
    [InlineData("../mp4")]
    [InlineData("..mp4")]
    [InlineData(".")]
    public void Load_RejectsContainerPathsAndMalformedExtensions(string extension)
    {
        WithPresetFile($$"""
            { "id": "custom", "displayName": "Custom", "output": { "containerExtension": "{{extension}}" } }
            """, path =>
        {
            var exception = Assert.Throws<InvalidDataException>(() => PresetCatalog.Load(presetFile: path));

            Assert.Contains("output.containerExtension", exception.Message);
        });
    }

    private static void WithPresetFile(string json, Action<string> assertion)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ovt-preset-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "custom.json");
            File.WriteAllText(path, json);
            assertion(path);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
