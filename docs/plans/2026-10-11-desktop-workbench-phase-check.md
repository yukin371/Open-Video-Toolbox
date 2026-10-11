# Desktop 工作台阶段验收记录

> 验收日期：2026-10-11
> 当前分支：`ui`
> 当前阶段：`D1-2 字幕与转写工作流`

## 验收范围

本记录只验收已经进入代码的 Desktop 工作台能力，并把尚未完成的需求明确留在 D1-2 后续工作面。

| 需求 | 状态 | 证据 |
| --- | --- | --- |
| 多文件导入与任务队列 | 通过 | `MainWindow.axaml.cs` 的 `OnAddVideosClick`、`QueueItems` |
| 媒体 probe | 通过 | `ProbeSelectedInputAsync` 使用 `FfprobeMediaProbeService` |
| 预设选择与命令预览 | 通过 | `TryBuildJob`、`FfmpegCommandBuilder` |
| 单任务执行与取消 | 通过 | `OnQueueClick`、`RunTaskAsync`、`OnCancelClick` |
| 批量串行执行 | 通过 | `OnRunQueueClick`；跳过成功项、失败后继续、取消后停止调度 |
| 队列本地持久化 | 通过 | `WorkbenchQueueStore`；保存状态、错误、日志、产物路径和字幕/转写路径 |
| SRT/VTT 字幕选择 | 通过 | `OnSubtitleClick`、`OnTranslationSubtitleClick` |
| 双语字幕合并 | 通过 | `SubtitleWorkflow.MergeBilingual` + `SubtitleDocumentRenderer.RenderSrt` |
| 本地 Whisper 转写 | 通过 | `WhisperCppTranscriptionService` + `OnTranscribeClick` |
| 外部翻译接入口 | 通过 | 第二份 SRT/VTT 字幕文件作为 `TranslationSubtitlePath` |
| sidecar/burn-in 实际渲染 | 通过 | `JobDefinition.SubtitleMode` 进入 `FfmpegCommandBuilder`；sidecar 由 `TranscodeJobRunner` 复制并记录产物 |
| 依赖状态面板 | 待完成 | 当前只在执行失败日志中体现外部工具错误 |
| 任务重试 / 工作目录 / edit.json 导出 | 待完成 | 保留在 D1-3 / D1-4 |

## 验证命令

```powershell
dotnet build src/OpenVideoToolbox.Desktop/OpenVideoToolbox.Desktop.csproj --no-restore --nologo
dotnet test OpenVideoToolbox.sln --no-restore --nologo
git diff --check
```

当前通过结果：

- Desktop 构建：0 警告、0 错误
- `OpenVideoToolbox.Core.Tests`：196/196
- `OpenVideoToolbox.Cli.Tests`：195/195

## D1-2 通过标准

- 用户可以为任务选择原字幕和外部翻译字幕。
- 用户可以复用 Core 字幕模型生成双语 SRT。
- 用户可以指定本地 Whisper 模型并生成转写 SRT。
- 模型路径、转写选项、字幕路径和任务状态会随队列保存并在下次启动恢复。
- 翻译仍由外部工具提供，仓库不内置 provider、SDK 或远程模型调用。
- `sidecar` 与 `burnIn` 均通过 Core 执行链生效，Desktop 不直接拼接 FFmpeg 参数。

## 下一张实现卡

下一步转入 D1-3 / D1-4：补依赖状态面板、失败重试、工作目录与 `edit.json` 导出；这些能力不影响已通过的 D1-2 字幕与转写闭环。
