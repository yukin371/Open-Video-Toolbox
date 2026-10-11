using System.Text.Json;

namespace OpenVideoToolbox.Desktop;

public sealed class WorkbenchQueueStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string FilePath { get; } = ResolveFilePath();

    public async Task<IReadOnlyList<WorkbenchTask>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath)) return [];
        await using var stream = File.OpenRead(FilePath);
        return await JsonSerializer.DeserializeAsync<List<WorkbenchTask>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    public async Task SaveAsync(IEnumerable<WorkbenchTask> tasks, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = FilePath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, tasks.ToArray(), JsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, FilePath, overwrite: true);
    }

    private static string ResolveFilePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root)) root = AppContext.BaseDirectory;
        return Path.Combine(root, "OpenVideoToolbox", "desktop-workbench.json");
    }
}
