using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OpenVideoToolbox.Desktop;

public enum WorkbenchTaskState
{
    Ready,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

public sealed class WorkbenchTask : INotifyPropertyChanged
{
    private WorkbenchTaskState _state = WorkbenchTaskState.Ready;
    private string? _errorMessage;
    private string[] _producedPaths = [];
    private string _executionLog = string.Empty;

    public required string Id { get; init; }
    public required string InputPath { get; init; }
    public required string PresetId { get; set; }
    public required string OutputDirectory { get; set; }
    public string? SubtitlePath { get; set; }
    public string? TranslationSubtitlePath { get; set; }
    public string SubtitleMode { get; set; } = "sidecar";
    public string? WhisperModelPath { get; set; }
    public string? TranscriptPath { get; set; }
    public string? TranscriptionLanguage { get; set; }
    public bool TranslateToEnglish { get; set; }
    public string? AudioPath { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public TimeSpan? Elapsed { get; set; }

    public WorkbenchTaskState State
    {
        get => _state;
        set => SetField(ref _state, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public string[] ProducedPaths
    {
        get => _producedPaths;
        set => SetField(ref _producedPaths, value ?? []);
    }

    public string ExecutionLog
    {
        get => _executionLog;
        set => SetField(ref _executionLog, value ?? string.Empty);
    }

    [JsonIgnore]
    public string DisplayName => $"{Path.GetFileName(InputPath)} · {StateLabel}";

    [JsonIgnore]
    public string StateLabel => State switch
    {
        WorkbenchTaskState.Ready => "待执行",
        WorkbenchTaskState.Running => "执行中",
        WorkbenchTaskState.Succeeded => "已完成",
        WorkbenchTaskState.Failed => "失败",
        WorkbenchTaskState.Cancelled => "已取消",
        _ => State.ToString()
    };

    public void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        OnPropertyChanged(nameof(UpdatedAtUtc));
        OnPropertyChanged(nameof(DisplayName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        if (propertyName is not null) OnPropertyChanged(propertyName);
        if (propertyName is nameof(State)) OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(DisplayName));
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
