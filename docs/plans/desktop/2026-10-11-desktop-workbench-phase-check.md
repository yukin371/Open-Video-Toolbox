# Desktop 工作台阶段验收记录

> 验收日期：2026-10-11
> 当前分支：`ui`
> 当前阶段：`D1-4 工作台收口`

## 验收范围

本记录只验收已经进入代码的 Desktop 工作台能力，并把尚未完成的需求明确留在 D1-2 后续工作面。

| 需求 | 状态 | 证据 |
| --- | --- | --- |
| 多文件导入与任务队列 | 通过 | `MainWindow.axaml.cs` 的 `OnAddVideosClick`、`QueueItems` |
| 目录导入 | 通过 | `OnAddDirectoryClick` 扫描目录内的视频文件 |
| 媒体 probe | 通过 | `ProbeSelectedInputAsync` 使用 `FfprobeMediaProbeService` |
| 预设选择与命令预览 | 通过 | `TryBuildJob`、`FfmpegCommandBuilder` |
| 默认压制方案 | 通过 | 内置兼容优先、快速压制、小体积 720p、H.265 体积优先和快速封装预设 |
| 单任务执行与取消 | 通过 | `OnQueueClick`、`RunTaskAsync`、`OnCancelClick` |
| 失败任务重试 | 通过 | `OnRetryClick` 重置任务状态并复用原任务配置 |
| 批量串行执行 | 通过 | `OnRunQueueClick`；跳过成功项、失败后继续、取消后停止调度 |
| 队列本地持久化 | 通过 | `WorkbenchQueueStore`；保存状态、错误、日志、产物路径和字幕/转写路径 |
| SRT/VTT 字幕选择 | 通过 | `OnSubtitleClick`、`OnTranslationSubtitleClick` |
| 双语字幕合并 | 通过 | `SubtitleWorkflow.MergeBilingual` + `SubtitleDocumentRenderer.RenderSrt` |
| 字幕截取 | 通过 | `SubtitleWorkflow.Clip` + `SubtitleDocumentRenderer.RenderSrt` |
| 本地 Whisper 转写 | 通过 | `WhisperCppTranscriptionService` + `OnTranscribeClick` |
| 外部翻译接入口 | 通过 | 第二份 SRT/VTT 字幕文件作为 `TranslationSubtitlePath` |
| sidecar/burn-in 实际渲染 | 通过 | `JobDefinition.SubtitleMode` 进入 `FfmpegCommandBuilder`；sidecar 由 `TranscodeJobRunner` 复制并记录产物 |
| `edit.json` 导出 | 通过 | `EditPlanTemplateFactory` 生成标准计划并由 `OpenVideoToolboxJson` 序列化 |
| 覆盖策略编辑 | 通过 | `OverwriteComboBox` 持久化到任务并传入 `JobOutput` |
| 依赖状态面板 | 通过 | 启动时通过 `ExternalDependencyInspector` 检查 ffmpeg、ffprobe 和可选 whisper-cli |
| 独立工作目录 | 通过 | `WorkbenchTask.WorkingDirectory` 持久化为 `.ovt-work/<task-id>`，并传入 `JobOutput.WorkingDirectory` |
| 最近任务恢复 | 通过 | 启动时 `WorkbenchQueueStore.LoadAsync` 恢复任务、状态、日志和产物 |
| 预设导入/导出 | 通过 | Desktop JSON 文件选择器支持单个或数组预设 |
| 输出目录快捷打开 | 通过 | `OnOpenOutputDirectoryClick` |
| Windows 发布 smoke | 通过 | `scripts/Test-DesktopPublish.ps1` 已验证 `win-x64` 产物含 exe 与 deps.json |

## 验证命令

```powershell
dotnet build src/OpenVideoToolbox.Desktop/OpenVideoToolbox.Desktop.csproj --no-restore --nologo
dotnet test OpenVideoToolbox.sln --no-restore --nologo
git diff --check
```

当前通过结果：

- Desktop 构建：0 警告、0 错误
- `OpenVideoToolbox.Core.Tests`：200/200
- `OpenVideoToolbox.Cli.Tests`：195/195

## D1-4 通过标准

- 用户可以为任务选择原字幕和外部翻译字幕。
- 用户可以复用 Core 字幕模型生成双语 SRT。
- 用户可以指定本地 Whisper 模型并生成转写 SRT。
- 模型路径、转写选项、字幕路径和任务状态会随队列保存并在下次启动恢复。
- 翻译仍由外部工具提供，仓库不内置 provider、SDK 或远程模型调用。
- `sidecar` 与 `burnIn` 均通过 Core 执行链生效，Desktop 不直接拼接 FFmpeg 参数。
- 最近任务、预设导入/导出、输出目录快捷打开和 Windows `win-x64` 发布 smoke 均有可复查入口。

## 下一张实现卡

D1-4 收口：后续只需按发布版本复核 smoke 产物，不再新增 Desktop 业务能力。
