# 系统架构与代码模块

> 最后更新：2026-10-13

本文描述当前实现中的模块、依赖方向和主要数据流。可视化图见 [架构总览](diagrams/architecture-overview.drawio)、[代码模块映射](diagrams/code-module-map.drawio) 与 [Wave 1 CLI 流程](diagrams/wave1-cli-flow.drawio)。模块职责与禁止事项以 [架构护栏](ARCHITECTURE_GUARDRAILS.md) 和各目录的 `MODULE.md` 为准。

## 架构原则

- `Core` 持有领域模型、媒体业务规则、命令计划和执行能力；`Cli` 与 `Desktop` 是调用入口。
- `Cli` 和 `Desktop` 可以依赖 `Core`，不能互相依赖。外部 AI 与脚本通过 CLI 和结构化文件接入。
- 外部进程由 `Core.Execution` 统一构建和执行，负责超时、取消、标准输出、标准错误及执行结果；领域适配模块负责各自的结构化解析。
- `edit.json` 模型、校验、模板和素材修改规则归 `Core.Editing`；UI 瞬时状态不进入该格式。
- JSON 序列化选项由 `Core.Serialization` 统一维护，命令和 UI 不复制核心模型语义。

## 分层与模块职责

| 层 | 代码位置 | 职责 |
| --- | --- | --- |
| 命令行入口 | `src/OpenVideoToolbox.Cli` | 参数解析、命令分发、路径与 manifest glue、稳定 JSON envelope、退出码；组合 Core 能力，不拥有领域规则。 |
| 桌面入口 | `src/OpenVideoToolbox.Desktop` | 本地工作台、任务队列和偏好设置；直接调用 Core，不绕过 Core 执行器。它不属于 v1.0 正式发布面，当前产品范围见 Roadmap。 |
| 核心媒体模型 | `Core/Media` | 调用 ffprobe，解析其 JSON 并生成统一的 `MediaProbeResult`。 |
| 任务和预设 | `Core/Jobs`、`Core/Presets` | `JobDefinition` 描述输入、输出和一次转码任务；预设模型、内置目录、自定义预设加载与校验归 Presets。 |
| 计划模型 | `Core/Editing` | `EditPlan` / timeline 模型、模板、校验、巡检、素材替换与挂载、确定性草稿生成。 |
| 外部工具执行 | `Core/Execution` | 命令构建、预览、进程执行、超时/取消、完整日志与 `ExecutionResult`；承接 ffmpeg、ffprobe 及适配器要求的外部工具调用。 |
| 字幕与语音 | `Core/Subtitles`、`Core/Speech` | Subtitles 定义 transcript / 字幕数据与确定性文件转换；Speech 将 whisper.cpp 输出映射为标准 transcript。 |
| 音频与信号 | `Core/Audio`、`Core/AudioSeparation`、`Core/Beats` | 音量/静音结果解析、人声分离结果映射、统一 PCM 上的节拍分析。 |
| 序列化 | `Core/Serialization` | 共享 JSON 配置和序列化约定。 |
| 自动化验证 | `src/OpenVideoToolbox.Core.Tests`、`src/OpenVideoToolbox.Cli.Tests` | Core 模型与执行构建验证；CLI 命令输出、退出码和契约快照验证。 |

## 依赖方向与协作

- `Cli → Core`：组合 Media、Presets、Jobs、Editing、Execution、Subtitles、Speech、Audio、AudioSeparation、Beats 和 Serialization，负责输入输出适配。
- `Desktop → Core`：复用媒体探测、预设、计划和执行能力；工作台状态与偏好属于 Desktop，不传入 Core 领域模型。
- `Core.Jobs → Core.Media / Core.Presets`：任务快照引用媒体探测结果与预设。
- `Core.Execution → Core.Jobs / Core.Presets / Core.Editing`：将任务或编辑计划投影为命令计划并执行；v1 clips 与 v2 timeline 的 render 路径均由此承接。
- `Core.Media → Core.Execution`：通过统一进程抽象调用 ffprobe，并从其输出建立媒体模型。
- `Core.Speech → Core.Subtitles / Core.Execution`：执行转写适配并将输出映射到标准 transcript。
- 音频、分离、字幕、节拍和 Editing 模块维护各自的数据语义；外部命令执行仍复用 Execution。
- `Core.Serialization` 被需要稳定 JSON 读写的模块复用，不负责领域校验。

以上是协作关系的概览，不替代模块级依赖与不变量文档。不得在入口层复制命令构建、计划校验或领域解析逻辑。

## 关键数据流

### 媒体探测与压缩

调用方提供媒体路径和预设；CLI 解析选项，Core.Media 通过 Core.Execution 调用 ffprobe 并生成 `MediaProbeResult`。Core.Presets 解析内置或显式提供的预设，CLI 组合出 `JobDefinition`；Core.Execution 将任务转换为 `CommandPlan`，通过统一进程执行器运行 ffmpeg，返回包含状态、退出码、耗时、完整输出日志和产物路径的 `ExecutionResult`。CLI 再输出结构化结果。

当前 `run` 由 `Core.Execution.TranscodeJobRunner` 先写随机临时产物，再比较源文件与结果大小。默认情况下，较大的结果会被拒绝并清理临时文件；`--allow-larger-output` 只作为显式 override。源文件、已有正式输出和 sidecar 在检查失败时保持不变，`ExecutionResult.OutputSizeComparison` 与 diagnostics 提供结构化证据。

### edit.json 生成、修改与渲染

外部脚本、AI 或用户生成并编辑 `edit.json`。CLI 负责读取文件、解析相对路径并调用 Core.Editing；该模块负责模板生成、模型校验、素材巡检和绑定修改。渲染时 CLI 将计划交给 Core.Execution，由执行层构建 preview 或实际命令并运行。

当计划包含 v2 timeline 时，Core.Execution 在统一的渲染入口内选择对应 builder；CLI 不判断时间线细节、不拼接 FFmpeg 参数。render preview 与执行共享命令计划及产物规则。

### 批量命令

CLI 解析 batch manifest，将路径按 manifest 约定解析，然后逐项复用对应的单项命令路径。CLI 负责批量结果汇总、`summary.json`、`results/<id>.json` 与退出码；单项任务、plan 或 render 的领域语义仍由各自 Core owner 持有。批处理不引入第二份任务模型。

### 转写、字幕与 supporting signals

CLI 组合 Core.Execution 的媒体/音频提取与外部工具调用、Core.Speech 的 transcript 映射、Core.Subtitles 的字幕文件转换，以及 Core.Audio、Core.AudioSeparation 和 Core.Beats 的分析结果。产物通过 CLI 明确参数或 Core.Editing 的 plan 绑定进入后续流程；不会隐式扫描文件或内置远程 AI provider。

## 关键对象的 canonical owner

| 对象或能力 | Owner |
| --- | --- |
| `MediaProbeResult` | `Core.Media` |
| `PresetDefinition` 与预设目录 | `Core.Presets` |
| `JobDefinition` | `Core.Jobs` |
| `EditPlan`、模板与校验 | `Core.Editing` |
| `CommandPlan`、`ExecutionResult`、原始进程输出 | `Core.Execution` |
| transcript 与字幕文件模型 | `Core.Subtitles` |
| whisper.cpp JSON 适配 | `Core.Speech` |
| audio / silence 分析语义 | `Core.Audio` |
| Demucs stem 结果映射 | `Core.AudioSeparation` |
| PCM 节拍分析 | `Core.Beats` |
| batch manifest 命令封装与汇总 | `Cli` |
| 桌面任务列表、工作台偏好和界面状态 | `Desktop` |

## 文档边界

- 本文维护当前系统结构、依赖方向和数据流。
- [架构护栏](ARCHITECTURE_GUARDRAILS.md)维护长期约束与禁止事项。
- 每个 `MODULE.md` 维护局部 owner、依赖、不变量和文档同步条件。
- 当前优先级以 [Roadmap](roadmap.md) 为准；专项范围和验收记录以 [计划索引](plans/README.md) 中对应卡片为准。
