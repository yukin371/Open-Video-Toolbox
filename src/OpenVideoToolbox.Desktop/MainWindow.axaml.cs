using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace OpenVideoToolbox.Desktop;

public partial class MainWindow : Window
{
    public ObservableCollection<string> QueueItems { get; } = [];
    private string? _selectedInput;
    private string? _subtitlePath;
    private string? _audioPath;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void OnAddVideosClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择视频文件",
            AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType("视频") { Patterns = ["*.mp4", "*.mov", "*.mkv", "*.webm", "*.avi"] }]
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path) && !QueueItems.Contains(path)) QueueItems.Add(path);
        }

        if (QueueItems.Count > 0)
        {
            _selectedInput = QueueItems[0];
            SelectionText.Text = _selectedInput;
            StatusText.Text = $"已添加 {QueueItems.Count} 个文件";
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 已载入媒体文件。{Environment.NewLine}下一步可选择预设并加入队列。";
        }

        QueueCountText.Text = QueueItems.Count.ToString();
    }

    private void OnClearQueueClick(object? sender, RoutedEventArgs e)
    {
        QueueItems.Clear();
        _selectedInput = null;
        SelectionText.Text = "尚未选择输入文件";
        QueueCountText.Text = "0";
        StatusText.Text = "就绪";
        LogText.Text = "等待选择媒体文件。";
    }

    private async void OnSubtitleClick(object? sender, RoutedEventArgs e)
    {
        _subtitlePath = await PickSingleFileAsync("选择字幕文件", ["*.srt", "*.vtt", "*.ass"]);
        UpdateMaterialText();
    }

    private async void OnAudioClick(object? sender, RoutedEventArgs e)
    {
        _audioPath = await PickSingleFileAsync("选择音频文件", ["*.wav", "*.mp3", "*.m4a", "*.flac"]);
        UpdateMaterialText();
    }

    private void OnQueueClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedInput))
        {
            StatusText.Text = "请先选择输入文件";
            return;
        }

        StatusText.Text = "已加入待执行队列";
        LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 已创建工作台任务。{Environment.NewLine}执行服务接入将在下一阶段连接 Core.Execution。";
    }

    private async Task<string?> PickSingleFileAsync(string title, IReadOnlyList<string> patterns)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(title) { Patterns = patterns }]
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private void UpdateMaterialText()
    {
        var materials = new[] { _subtitlePath is null ? null : $"字幕：{_subtitlePath}", _audioPath is null ? null : $"音频：{_audioPath}" }
            .Where(value => value is not null);
        MaterialText.Text = string.Join(Environment.NewLine, materials.DefaultIfEmpty("未附加字幕或音频"));
    }
}
