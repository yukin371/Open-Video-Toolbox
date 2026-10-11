using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;
using OpenVideoToolbox.Core.Subtitles;
using OpenVideoToolbox.Core.Speech;

namespace OpenVideoToolbox.Desktop;

public partial class MainWindow : Window
{
    public ObservableCollection<WorkbenchTask> QueueItems { get; } = [];

    private readonly WorkbenchQueueStore _queueStore = new();
    private CancellationTokenSource? _executionCancellation;
    private WorkbenchTask? _selectedTask;
    private MediaProbeResult? _probe;
    private bool _queueRunning;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        PresetComboBox.ItemsSource = BuiltInPresetCatalog.GetAll();
        PresetComboBox.SelectedItem = BuiltInPresetCatalog.GetRequired(BuiltInPresetCatalog.DefaultPresetId);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var task in await _queueStore.LoadAsync()) QueueItems.Add(task);
            UpdateQueueCount();
            if (QueueItems.Count > 0)
            {
                QueueList.SelectedIndex = 0;
                LogText.Text = $"已恢复 {QueueItems.Count} 个任务。\n队列文件：{_queueStore.FilePath}";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "队列恢复失败";
            LogText.Text = $"{ex.Message}\n队列文件：{_queueStore.FilePath}";
        }
    }

    private async void OnAddVideosClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择视频文件",
            AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType("视频") { Patterns = ["*.mp4", "*.mov", "*.mkv", "*.webm", "*.avi"] }]
        });

        var defaultPreset = BuiltInPresetCatalog.DefaultPresetId;
        var outputDirectory = ResolveOutputDirectory(OutputDirectoryTextBox.Text);
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path) || QueueItems.Any(task => string.Equals(task.InputPath, path, StringComparison.OrdinalIgnoreCase))) continue;
            QueueItems.Add(new WorkbenchTask
            {
                Id = $"desktop-{Guid.NewGuid():N}",
                InputPath = Path.GetFullPath(path),
                PresetId = defaultPreset,
                OutputDirectory = outputDirectory
            });
        }

        UpdateQueueCount();
        if (_selectedTask is null && QueueItems.Count > 0) QueueList.SelectedIndex = 0;
        StatusText.Text = QueueItems.Count > 0 ? $"已添加 {QueueItems.Count} 个文件" : "就绪";
        LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 已载入媒体文件。\n队列会自动保存到：{_queueStore.FilePath}";
        await PersistQueueAsync();
    }

    private async void OnQueueSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (QueueList.SelectedItem is not WorkbenchTask task) return;
        _selectedTask = task;
        _probe = null;
        OutputDirectoryTextBox.Text = task.OutputDirectory;
        SubtitleModeComboBox.SelectedIndex = string.Equals(task.SubtitleMode, "burnIn", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        WhisperModelPathTextBox.Text = task.WhisperModelPath ?? string.Empty;
        TranscriptionLanguageTextBox.Text = task.TranscriptionLanguage ?? string.Empty;
        TranslateToEnglishCheckBox.IsChecked = task.TranslateToEnglish;
        PresetComboBox.SelectedItem = BuiltInPresetCatalog.TryGet(task.PresetId, out var preset)
            ? preset
            : BuiltInPresetCatalog.GetRequired(BuiltInPresetCatalog.DefaultPresetId);
        MaterialText.Text = BuildMaterialText(task);
        SelectionText.Text = task.InputPath;
        StatusText.Text = task.StateLabel;
        UpdateLogForTask(task);
        await ProbeSelectedInputAsync();
    }

    private async void OnClearQueueClick(object? sender, RoutedEventArgs e)
    {
        _executionCancellation?.Cancel();
        QueueItems.Clear();
        _selectedTask = null;
        _probe = null;
        UpdateQueueCount();
        SelectionText.Text = "尚未选择输入文件";
        StatusText.Text = "就绪";
        LogText.Text = "队列已清空。";
        await PersistQueueAsync();
    }

    private async void OnSubtitleClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedTask is null) return;
        _selectedTask.SubtitlePath = await PickSingleFileAsync("选择字幕文件", ["*.srt", "*.vtt", "*.ass"]);
        _selectedTask.Touch();
        MaterialText.Text = BuildMaterialText(_selectedTask);
        await PersistQueueAsync();
    }

    private async void OnTranslationSubtitleClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedTask is null) return;
        _selectedTask.TranslationSubtitlePath = await PickSingleFileAsync("选择翻译字幕文件", ["*.srt", "*.vtt"]);
        _selectedTask.Touch();
        MaterialText.Text = BuildMaterialText(_selectedTask);
        await PersistQueueAsync();
    }

    private async void OnMergeSubtitlesClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedTask is null) return;
        if (string.IsNullOrWhiteSpace(_selectedTask.SubtitlePath) || string.IsNullOrWhiteSpace(_selectedTask.TranslationSubtitlePath))
        {
            StatusText.Text = "双语字幕合并失败";
            LogText.Text = "请先选择原字幕和翻译字幕文件。";
            return;
        }

        try
        {
            var primary = ParseSubtitleFile(_selectedTask.SubtitlePath);
            var secondary = ParseSubtitleFile(_selectedTask.TranslationSubtitlePath);
            var merged = SubtitleWorkflow.MergeBilingual(primary, secondary);
            var outputDirectory = ResolveOutputDirectory(_selectedTask.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(_selectedTask.InputPath)}-bilingual.srt");
            await File.WriteAllTextAsync(outputPath, SubtitleDocumentRenderer.RenderSrt(merged));
            _selectedTask.SubtitlePath = outputPath;
            _selectedTask.SubtitleMode = GetSelectedSubtitleMode();
            _selectedTask.Touch();
            MaterialText.Text = BuildMaterialText(_selectedTask);
            StatusText.Text = "双语字幕已生成";
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 双语字幕合并完成\n输出：{outputPath}\n字幕条数：{merged.Cues.Count}";
            await PersistQueueAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = "双语字幕合并失败";
            LogText.Text = ex.Message;
        }
    }

    private async void OnAudioClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedTask is null) return;
        _selectedTask.AudioPath = await PickSingleFileAsync("选择音频文件", ["*.wav", "*.mp3", "*.m4a", "*.flac"]);
        _selectedTask.Touch();
        MaterialText.Text = BuildMaterialText(_selectedTask);
        await PersistQueueAsync();
    }

    private async void OnWhisperModelClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedTask is null) return;
        _selectedTask.WhisperModelPath = await PickSingleFileAsync("选择 Whisper 模型", ["*.bin", "*.gguf"]);
        WhisperModelPathTextBox.Text = _selectedTask.WhisperModelPath ?? string.Empty;
        _selectedTask.Touch();
        await PersistQueueAsync();
    }

    private async void OnTranscribeClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedTask is null) return;
        var modelPath = WhisperModelPathTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            StatusText.Text = "转写失败";
            LogText.Text = "请先选择 Whisper 模型文件。";
            return;
        }

        if (_queueRunning) return;
        _selectedTask.WhisperModelPath = Path.GetFullPath(modelPath);
        _selectedTask.TranscriptionLanguage = string.IsNullOrWhiteSpace(TranscriptionLanguageTextBox.Text)
            ? null
            : TranscriptionLanguageTextBox.Text.Trim();
        _selectedTask.TranslateToEnglish = TranslateToEnglishCheckBox.IsChecked == true;
        _executionCancellation?.Dispose();
        _executionCancellation = new CancellationTokenSource();
        try
        {
            StatusText.Text = "正在转写";
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 开始本地转写\n模型：{_selectedTask.WhisperModelPath}";
            await PersistQueueAsync();

            var service = new WhisperCppTranscriptionService(
                new AudioWaveformExtractRunner(new FfmpegAudioWaveformExtractCommandBuilder(), new DefaultProcessRunner()),
                new WhisperCppTranscriptionRunner(new WhisperCppCommandBuilder(), new DefaultProcessRunner()),
                new WhisperCppJsonParser());
            var transcript = await service.TranscribeAsync(
                new WhisperCppTranscriptionRequest
                {
                    InputPath = _selectedTask.InputPath,
                    ModelPath = _selectedTask.WhisperModelPath,
                    Language = _selectedTask.TranscriptionLanguage,
                    TranslateToEnglish = _selectedTask.TranslateToEnglish
                },
                cancellationToken: _executionCancellation.Token);

            var outputDirectory = ResolveOutputDirectory(_selectedTask.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(_selectedTask.InputPath)}-transcript.srt");
            var rendered = new SubtitleRenderer().Render(new SubtitleRenderRequest
            {
                Transcript = transcript,
                OutputPath = outputPath,
                Format = SubtitleFormat.Srt
            });
            await File.WriteAllTextAsync(outputPath, rendered.Content, _executionCancellation.Token);
            _selectedTask.TranscriptPath = outputPath;
            _selectedTask.SubtitlePath = outputPath;
            _selectedTask.Touch();
            MaterialText.Text = BuildMaterialText(_selectedTask);
            StatusText.Text = "转写完成";
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 本地转写完成\n语言：{transcript.Language ?? "未知"}\n片段：{transcript.Segments.Count}\n字幕：{outputPath}";
            await PersistQueueAsync();
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "转写已取消";
            LogText.Text = $"{LogText.Text}\n[{DateTime.Now:HH:mm:ss}] 已取消转写。";
        }
        catch (Exception ex)
        {
            StatusText.Text = "转写失败";
            LogText.Text = ex.Message;
        }
        finally
        {
            _executionCancellation?.Dispose();
            _executionCancellation = null;
        }
    }

    private async void OnPreviewClick(object? sender, RoutedEventArgs e)
    {
        if (!TryBuildJob(out var task, out var job, out var error))
        {
            StatusText.Text = "预览失败";
            LogText.Text = error;
            return;
        }

        var plan = new FfmpegCommandBuilder().Build(job);
        StatusText.Text = "命令预览已生成";
        LogText.Text = $"[{DateTime.Now:HH:mm:ss}] 预览\n{plan.CommandLine}";
        task.Touch();
        await PersistQueueAsync();
    }

    private async void OnQueueClick(object? sender, RoutedEventArgs e)
    {
        if (!TryBuildJob(out var task, out var job, out var error))
        {
            StatusText.Text = "执行失败";
            LogText.Text = error;
            return;
        }

        if (_queueRunning) return;
        _executionCancellation?.Dispose();
        _executionCancellation = new CancellationTokenSource();
        CancelButton.IsEnabled = true;
        try
        {
            await RunTaskAsync(task, job, _executionCancellation.Token);
        }
        finally
        {
            CancelButton.IsEnabled = false;
        }
    }

    private async void OnRunQueueClick(object? sender, RoutedEventArgs e)
    {
        if (_queueRunning || QueueItems.Count == 0) return;

        _queueRunning = true;
        _executionCancellation?.Dispose();
        _executionCancellation = new CancellationTokenSource();
        RunQueueButton.IsEnabled = false;
        CancelButton.IsEnabled = true;

        try
        {
            foreach (var task in QueueItems.ToArray())
            {
                if (task.State == WorkbenchTaskState.Succeeded) continue;
                if (_executionCancellation.IsCancellationRequested) break;

                _selectedTask = task;
                SelectionText.Text = task.InputPath;
                StatusText.Text = task.StateLabel;
                UpdateLogForTask(task);
                var preset = BuiltInPresetCatalog.TryGet(task.PresetId, out var selectedPreset)
                    ? selectedPreset
                    : BuiltInPresetCatalog.GetRequired(BuiltInPresetCatalog.DefaultPresetId);
                var job = BuildJob(task, preset, null);
                await RunTaskAsync(task, job, _executionCancellation.Token);
            }
        }
        finally
        {
            _queueRunning = false;
            RunQueueButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
            ProgressBar.IsIndeterminate = false;
            await PersistQueueAsync();
        }
    }

    private async Task RunTaskAsync(WorkbenchTask task, JobDefinition job, CancellationToken cancellationToken)
    {
        var commandLine = new FfmpegCommandBuilder().Build(job).CommandLine;
        var log = $"[{DateTime.Now:HH:mm:ss}] 开始执行\n{commandLine}";
        try
        {
            Directory.CreateDirectory(job.Output.OutputDirectory);
            task.State = WorkbenchTaskState.Running;
            task.ErrorMessage = null;
            task.ProducedPaths = [];
            task.ExecutionLog = log;
            task.Touch();
            StatusText.Text = $"正在执行：{Path.GetFileName(task.InputPath)}";
            LogText.Text = log;
            ProgressBar.IsIndeterminate = true;
            await PersistQueueAsync();

            var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(job, cancellationToken: cancellationToken);
            task.State = result.Status switch
            {
                ExecutionStatus.Succeeded => WorkbenchTaskState.Succeeded,
                ExecutionStatus.Cancelled => WorkbenchTaskState.Cancelled,
                _ => WorkbenchTaskState.Failed
            };
            task.ErrorMessage = result.ErrorMessage;
            task.ProducedPaths = result.ProducedPaths.ToArray();
            var output = string.Join(Environment.NewLine, result.OutputLines.Select(line => $"[{line.Channel}] {line.Text}"));
            task.ExecutionLog = $"{log}{Environment.NewLine}{output}{Environment.NewLine}状态：{result.Status}{Environment.NewLine}输出：{string.Join(", ", result.ProducedPaths)}";
            LogText.Text = task.ExecutionLog;
        }
        catch (OperationCanceledException)
        {
            task.State = WorkbenchTaskState.Cancelled;
            task.ErrorMessage = "执行已取消。";
            task.ExecutionLog = $"{log}{Environment.NewLine}状态：Cancelled";
            LogText.Text = task.ExecutionLog;
        }
        catch (Exception ex)
        {
            task.State = WorkbenchTaskState.Failed;
            task.ErrorMessage = ex.Message;
            task.ExecutionLog = $"{log}{Environment.NewLine}错误：{ex.Message}";
            LogText.Text = task.ExecutionLog;
        }
        finally
        {
            task.Touch();
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = task.State == WorkbenchTaskState.Succeeded ? 100 : 0;
            StatusText.Text = task.StateLabel;
            await PersistQueueAsync();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _executionCancellation?.Cancel();
        StatusText.Text = "正在取消";
        LogText.Text = $"{LogText.Text}{Environment.NewLine}[{DateTime.Now:HH:mm:ss}] 已请求取消执行。";
    }

    private bool TryBuildJob(out WorkbenchTask task, out JobDefinition job, out string error)
    {
        task = _selectedTask!;
        job = null!;
        error = string.Empty;
        if (task is null)
        {
            error = "请先从任务队列选择输入文件。";
            return false;
        }

        var preset = PresetComboBox.SelectedItem as PresetDefinition;
        if (preset is null)
        {
            error = "请选择处理预设。";
            return false;
        }

        task.PresetId = preset.Id;
        task.OutputDirectory = ResolveOutputDirectory(OutputDirectoryTextBox.Text);
        task.SubtitleMode = GetSelectedSubtitleMode();
        job = BuildJob(task, preset, _probe);
        task.Touch();
        return true;
    }

    private static JobDefinition BuildJob(WorkbenchTask task, PresetDefinition preset, MediaProbeResult? probe) => new()
    {
        Id = task.Id,
        CreatedAtUtc = task.CreatedAtUtc,
        Source = new JobSource { InputPath = task.InputPath },
        Output = new JobOutput
        {
            OutputDirectory = task.OutputDirectory,
            FileNameStem = $"{Path.GetFileNameWithoutExtension(task.InputPath)}-{preset.Id}",
            ContainerExtension = preset.Output.ContainerExtension,
            OverwriteExisting = preset.Output.OverwriteExisting
        },
        Preset = preset,
        ProbeSnapshot = probe,
        SubtitlePath = task.SubtitlePath,
        SubtitleMode = task.SubtitleMode
    };

    private async Task ProbeSelectedInputAsync()
    {
        if (_selectedTask is null) return;
        try
        {
            StatusText.Text = "正在探测媒体";
            _probe = await new FfprobeMediaProbeService(new DefaultProcessRunner(), new FfprobeJsonParser()).ProbeAsync(_selectedTask.InputPath);
            var duration = _probe.Format.Duration?.ToString() ?? "未知时长";
            var streams = string.Join(", ", _probe.Streams.GroupBy(stream => stream.Kind).Select(group => $"{group.Key}:{group.Count()}"));
            SelectionText.Text = $"{_probe.FileName} · {duration} · {streams}";
            StatusText.Text = _selectedTask.StateLabel;
            LogText.Text = $"[{DateTime.Now:HH:mm:ss}] ffprobe 完成\n容器：{_probe.Format.ContainerName ?? "未知"}\n流：{streams}";
        }
        catch (Exception ex)
        {
            _probe = null;
            StatusText.Text = "媒体探测失败";
            LogText.Text = ex.Message;
        }
    }

    private async Task PersistQueueAsync() => await _queueStore.SaveAsync(QueueItems);

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

    private static string ResolveOutputDirectory(string? value) => Path.GetFullPath(string.IsNullOrWhiteSpace(value) ? "exports" : value.Trim());

    private static string BuildMaterialText(WorkbenchTask task)
    {
        var materials = new[]
        {
            task.SubtitlePath is null ? null : $"字幕：{task.SubtitlePath}",
            task.TranslationSubtitlePath is null ? null : $"翻译字幕：{task.TranslationSubtitlePath}",
            task.AudioPath is null ? null : $"音频：{task.AudioPath}"
        }.Where(value => value is not null);
        return string.Join(Environment.NewLine, materials.DefaultIfEmpty("未附加字幕或音频"));
    }

    private static SubtitleDocument ParseSubtitleFile(string path)
    {
        var format = Path.GetExtension(path).TrimStart('.');
        return SubtitleDocumentParser.Parse(File.ReadAllText(path), format);
    }

    private string GetSelectedSubtitleMode()
    {
        return (SubtitleModeComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "sidecar";
    }

    private void UpdateLogForTask(WorkbenchTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.ExecutionLog))
        {
            LogText.Text = task.ExecutionLog;
            return;
        }
        var output = task.ProducedPaths.Length == 0 ? "" : $"\n输出：{string.Join(", ", task.ProducedPaths)}";
        LogText.Text = $"状态：{task.StateLabel}{output}{(string.IsNullOrWhiteSpace(task.ErrorMessage) ? "" : $"\n错误：{task.ErrorMessage}")}";
    }

    private void UpdateQueueCount() => QueueCountText.Text = QueueItems.Count.ToString();
}
