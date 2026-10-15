using System.Text.Json;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Core.Presets;

/// <summary>Combines built-in presets with explicitly supplied user preset files.</summary>
public sealed class PresetCatalog
{
    private readonly IReadOnlyDictionary<string, PresetDefinition> _presets;

    private PresetCatalog(IEnumerable<PresetDefinition> presets)
    {
        _presets = presets.ToDictionary(preset => preset.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<PresetDefinition> GetAll() => _presets.Values.OrderBy(preset => preset.Id).ToArray();

    public bool TryGet(string id, out PresetDefinition preset) => _presets.TryGetValue(id, out preset!);

    public PresetDefinition GetRequired(string id)
    {
        if (TryGet(id, out var preset))
        {
            return preset;
        }

        throw new KeyNotFoundException($"Unknown preset '{id}'.");
    }

    public static PresetCatalog Load(string? presetDirectory = null, string? presetFile = null)
    {
        var presets = BuiltInPresetCatalog.GetAll().ToList();
        if (!string.IsNullOrWhiteSpace(presetDirectory))
        {
            var directory = Path.GetFullPath(presetDirectory);
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Preset directory '{directory}' does not exist.");
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                presets.AddRange(ReadPresetFile(path));
            }
        }

        if (!string.IsNullOrWhiteSpace(presetFile))
        {
            presets.AddRange(ReadPresetFile(Path.GetFullPath(presetFile)));
        }

        var duplicate = presets
            .GroupBy(preset => preset.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Duplicate preset id '{duplicate.Key}'.");
        }

        return new PresetCatalog(presets);
    }

    private static IReadOnlyList<PresetDefinition> ReadPresetFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Preset file '{path}' does not exist.", path);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            IReadOnlyList<PresetDefinition?> presets = root.ValueKind switch
            {
                JsonValueKind.Object => [root.Deserialize<PresetDefinition>(OpenVideoToolboxJson.Default)],
                JsonValueKind.Array => root.Deserialize<PresetDefinition?[]>(OpenVideoToolboxJson.Default)!,
                _ => throw new InvalidDataException($"Preset file '{path}' must contain a preset object or array.")
            };

            foreach (var preset in presets)
            {
                Validate(preset, path);
            }

            return presets.Select(preset => preset!).ToArray();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Preset file '{path}' contains invalid preset JSON: {ex.Message}", ex);
        }
    }

    private static void Validate(PresetDefinition? preset, string path)
    {
        if (preset is null || string.IsNullOrWhiteSpace(preset.Id) || string.IsNullOrWhiteSpace(preset.DisplayName))
        {
            throw new InvalidDataException($"Preset file '{path}' requires non-empty id and displayName.");
        }

        if (preset.SchemaVersion != SchemaVersions.V1 || !Enum.IsDefined(preset.Kind))
        {
            throw new InvalidDataException($"Preset '{preset.Id}' in '{path}' has an unsupported schemaVersion or kind.");
        }

        if (preset.Output is null || string.IsNullOrWhiteSpace(preset.Output.ContainerExtension))
        {
            throw new InvalidDataException($"Preset '{preset.Id}' in '{path}' requires output.containerExtension.");
        }

        var extension = preset.Output.ContainerExtension.StartsWith('.')
            ? preset.Output.ContainerExtension[1..]
            : preset.Output.ContainerExtension;
        if (extension.Length == 0 || !extension.All(char.IsAsciiLetterOrDigit))
        {
            throw new InvalidDataException($"Preset '{preset.Id}' in '{path}' has an invalid output.containerExtension.");
        }

        if ((preset.Video is not null && string.IsNullOrWhiteSpace(preset.Video.Encoder))
            || (preset.Audio is not null && string.IsNullOrWhiteSpace(preset.Audio.Encoder)))
        {
            throw new InvalidDataException($"Preset '{preset.Id}' in '{path}' requires non-empty encoder names.");
        }

        // File names must remain usable on every supported platform, including Windows.
        var suffix = preset.OutputNameSuffix ?? $"_{preset.Id}";
        if (string.IsNullOrWhiteSpace(suffix) || suffix.Any(character => char.IsControl(character) || "<>:\"/\\|?*".Contains(character)))
        {
            throw new InvalidDataException($"Preset '{preset.Id}' in '{path}' has an invalid outputNameSuffix or fallback id.");
        }
    }
}
