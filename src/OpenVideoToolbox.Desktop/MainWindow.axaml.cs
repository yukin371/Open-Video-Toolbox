using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;

namespace OpenVideoToolbox.Desktop;

public partial class MainWindow : Window
{
    public ObservableCollection<string> QueueItems { get; } = [];
    private string? _selectedInput;
    private string? _subtitlePath;
    private string? _audioPath;
    private CancellationTokenSource? _executionCancellation;
    private MediaProbeResult? _probe;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        PresetComboBox.ItemsSource = BuiltInPresetCatalog.GetAll();
        PresetComboBox.SelectedItem = BuiltInPresetCatalog.GetRequired(BuiltInPresetCatalog.DefaultPresetId);
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
            await ProbeSelectedInputAsync();
        }

        QueueCountText.Text = QueueItems.Count.ToString();
    }

    private void OnClearQueueClick(object? sender, RoutedEventArgs e)
    {
        QueueItems.Clear();
        _selectedInput = null;
        _probe = null;
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

    private void OnPreviewClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedInput))
        {
            StatusText.Text = "请先选择输入文件";
            return;
        }

        try
        {
            var job = BuildJob();
            var plan = new FfmpegCommandBuilder().Build(job);
            StatusText.Text = "命令预览已生成";
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 预览{Environment.NewLine}{plan.CommandLine}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "预览失败";
            LogText.Text = ex.Message;
        }
    }

    private async void OnQueueClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedInput))
        {
            StatusText.Text = "请先选择输入文件";
            return;
        }

        try
        {
            var job = BuildJob();
            Directory.CreateDirectory(job.Output.OutputDirectory);
            _executionCancellation?.Dispose();
            _executionCancellation = new CancellationTokenSource();
            StatusText.Text = "正在执行";
            ProgressBar.IsIndeterminate = true;
            CancelButton.IsEnabled = true;
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 开始执行{Environment.NewLine}{new FfmpegCommandBuilder().Build(job).CommandLine}";

            var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(job, cancellationToken: _executionCancellation.Token);
            ProgressBar.IsIndeterminate = false;
            CancelButton.IsEnabled = false;
            ProgressBar.Value = result.Status == ExecutionStatus.Succeeded ? 100 : 0;
            StatusText.Text = result.Status switch
            {
                ExecutionStatus.Succeeded => "执行成功",
                ExecutionStatus.Cancelled => "已取消",
                _ => "执行失败"
            };
            var output = string.Join(Environment.NewLine, result.OutputLines.Select(line => $"[{line.Channel}] {line.Text}"));
            LogText.Text = $"{LogText.Text}{Environment.NewLine}{output}{Environment.NewLine}状态：{result.Status}{Environment.NewLine}输出：{string.Join(", ", result.ProducedPaths)}";
        }
        catch (Exception ex)
        {
            ProgressBar.IsIndeterminate = false;
            CancelButton.IsEnabled = false;
            StatusText.Text = "执行失败";
            LogText.Text = ex.Message;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _executionCancellation?.Cancel();
        StatusText.Text = "正在取消";
        LogText.Text = $"{LogText.Text}{Environment.NewLine}[{DateTime.Now:HH:mm:ss}] 已请求取消执行。";
    }

    private JobDefinition BuildJob()
    {
        var inputPath = Path.GetFullPath(_selectedInput!);
        var preset = PresetComboBox.SelectedItem as PresetDefinition
            ?? BuiltInPresetCatalog.GetRequired(BuiltInPresetCatalog.DefaultPresetId);
        var outputDirectory = Path.GetFullPath(OutputDirectoryTextBox.Text?.Trim() ?? "exports");
        var fileStem = Path.GetFileNameWithoutExtension(inputPath);
        return new JobDefinition
        {
            Id = $"desktop-{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Source = new JobSource { InputPath = inputPath },
            Output = new JobOutput
            {
                OutputDirectory = outputDirectory,
                FileNameStem = $"{fileStem}-{preset.Id}",
                ContainerExtension = preset.Output.ContainerExtension,
                OverwriteExisting = preset.Output.OverwriteExisting
            },
            Preset = preset,
            ProbeSnapshot = _probe
        };
    }

    private async Task ProbeSelectedInputAsync()
    {
        try
        {
            StatusText.Text = "正在探测媒体";
            _probe = await new FfprobeMediaProbeService(new DefaultProcessRunner(), new FfprobeJsonParser())
                .ProbeAsync(_selectedInput!);
            var duration = _probe.Format.Duration?.ToString() ?? "未知时长";
            var streams = string.Join(", ", _probe.Streams.GroupBy(stream => stream.Kind).Select(group => $"{group.Key}:{group.Count()}"));
            SelectionText.Text = $"{_probe.FileName} · {duration} · {streams}";
            StatusText.Text = "媒体已探测";
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] ffprobe 完成{Environment.NewLine}容器：{_probe.Format.ContainerName ?? "未知"}{Environment.NewLine}流：{streams}";
        }
        catch (Exception ex)
        {
            _probe = null;
            StatusText.Text = "媒体探测失败";
            LogText.Text = ex.Message;
        }
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
