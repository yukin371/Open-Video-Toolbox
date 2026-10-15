# Desktop 原型对齐与发布功能差距分析

最后更新：2026-10-11

## 1. 本文目的

`docs/原型/index.html` 是一版新的 Desktop 界面设计探索（品牌名 `FrameForge`，深色主题 + 侧边导航）。这份文档只回答两个问题：

1. 原型里的每个模块，**实际有哪些 Core / Desktop 能力可以对接**；
2. 要让原型成为"可对接、可发布"的设计，**还缺哪些功能**，分别属于谁。

本文不是实施卡。它不改变当前 `D1-4 收口` 阶段状态，也不把 `Desktop` 抬进 `v1.0` 正式支持面。它只用于把原型和真实能力对齐。

## 2. 当前实际基线（已核实）

### 2.1 发布面

| 项目 | 实际状态 | 证据 |
| --- | --- | --- |
| 公开版本 | `v0.1.0` 已发布，本地唯一 tag `v0.1.0` | `git tag`、GitHub Release |
| 目标版本 | `v1.0.0` 未发布 | `OpenVideoToolbox.Cli.csproj` 已为 `VersionPrefix 1.0.0`，tag / Release 仍待签发 |
| 发布结论 | `Conditional Go`，未最终签发 | `docs/plans/v1.0/2026-04-26-v1-0-go-no-go-signoff-draft.md` |
| 正式支持面 | 仅 `CLI + Core` | 同上 |
| Desktop | 明确**不在** `v1.0.0` 正式支持面 | 同上 |
| 当前测试基线（本次实测） | `396 passed`：`Core.Tests 201` + `Cli.Tests 195` | `dotnet test .\OpenVideoToolbox.sln`，2026-10-11 |
| 发布文档记录的测试基线 | `388 passed`：`Core.Tests 193` + `Cli.Tests 195` | v1.0 预检卡 / 签发稿 |

> 测试基线已从发布文档记录的 `388` 增至 `396`：`D1-4` 期间的 `0856cb1 feat(subtitles): 新增字幕导入截取与双语合并能力` 为 `Core.Tests` 增加了 8 个用例。数量变化不构成契约 breaking，但发布证据包在最终签发前应复核为当前数值。
>
> `OpenVideoToolbox.Desktop` **没有独立测试项目**，即 Desktop 当前无自动化回归保护。

### 2.2 CLI 能力面

`docs/PROJECT_PROFILE.md` 记录的 40+ 条命令均已实现并可用。与原型相关的核心能力：

- `probe`（ffprobe 媒体探测）、`plan` / `run`（传统转码）
- `templates` / `init-plan` / `scaffold-template` / `validate-plan` / `render`
- `inspect-plan` / `replace-plan-material` / `attach-plan-material` / `bind-voice-track`
- `scaffold-template-batch` / `render-batch`
- `cut` / `concat` / `extract-audio`
- `audio-analyze` / `audio-gain` / `audio-normalize` / `detect-silence` / `auto-cut-silence`
- `transcribe` / `subtitle` / `subtitle-import` / `subtitle-clip` / `merge-subtitles`
- `separate-audio` / `beat-track` / `export` / `mix-audio`

### 2.3 Desktop 实际实现

单窗口三栏浅色工作台（`src/OpenVideoToolbox.Desktop/MainWindow.axaml`，235 行）：

- 顶部：添加媒体 / 添加目录 / 执行队列 / 清空队列
- 左栏：任务队列 `ListBox`，状态筛选按钮（**未接线**）
- 中栏：4 个 Tab — 视频压制 / 字幕 / 音频 / 计划
- 右栏：状态、进度条（仅 `IsIndeterminate`）、说明文字
- 底部：深色 `stdout` / `stderr` 日志面板

已真实接线的能力（`MainWindow.axaml.cs`，772 行）：

- 文件 / 目录导入、去重、队列持久化（`WorkbenchQueueStore`）
- `ffprobe` 探测、依赖状态检查（`ExternalDependencyInspector`）
- 预设选择、输出目录、覆盖策略
- 命令预览（`FfmpegCommandBuilder`）、执行（`TranscodeJobRunner`）、取消、重试
- 按顺序批量执行、失败继续
- 字幕选择、双语合并、字幕截取、`Whisper` 转写
- `edit.json` 导出、预设导入导出、打开输出目录

内置预设只有 5 个，全部为**软件编码**（`src/OpenVideoToolbox.Core/Presets/BuiltInPresetCatalog.cs`）：

| preset id | 编码器 | 容器 |
| --- | --- | --- |
| `h264-aac-mp4` | `libx264` CRF 23 | `.mp4` |
| `h264-aac-fast` | `libx264` veryfast CRF 24 | `.mp4` |
| `h264-aac-720p` | `libx264` CRF 24 + `scale=-2:720` | `.mp4` |
| `h265-aac-mp4` | `libx265` CRF 26 | `.mp4` |
| `copy-aac-mkv` | `copy`（不重编码视频） | `.mkv` |

**`nvenc` / `qsv` / `videotoolbox` / `amf` / `hwaccel` 在 `Core` 中完全不存在。**

## 3. 原型模块 → 实际能力 对接表

图例：🟢 已实现可直接对接 ｜ 🟡 需要补实现 ｜ 🔴 超出当前产品边界

| 原型模块 | 对接目标（真实 owner） | 状态 |
| --- | --- | --- |
| 拖入文件 / 浏览文件 / `MP4 MOV AVI MKV` | `OnAddVideosClick` 已支持同格式 | 🟢 |
| 添加目录 | `OnAddDirectoryClick` | 🟢 |
| 处理队列 + 状态（处理中 / 已完成 / 已暂停） | `WorkbenchTask` / `WorkbenchTaskState` / `QueueList` | 🟢 |
| 队列持久化 | `WorkbenchQueueStore`（`desktop-workbench.json`） | 🟢 |
| 取消执行 | `OnCancelClick` + `CancellationToken` | 🟢 |
| 失败后重试 | `OnRetryClick` | 🟢 |
| 运行日志 `stdout` / `stderr` | `ExecutionResult.OutputLines` 分通道 | 🟢 |
| 媒体信息行（分辨率 / 体积 / 编码） | `MediaProbeResult`（宽高、时长、编码均已解析） | 🟢 |
| 底部状态栏「系统就绪 / 依赖」 | `ExternalDependencyInspector` + `DependencyText` | 🟢 |
| 输出目录 | `OutputDirectoryTextBox` | 🟢 |
| 覆盖策略开关 | `OverwriteComboBox` + `OutputSettings.OverwriteExisting` | 🟢 |
| 新建处理任务弹窗 | 现为散落的顶部按钮，需整合为一个流程弹窗 | 🟢 |
| 快速预设卡片组 | `BuiltInPresetCatalog.GetAll()`（当前仅 5 个，映射需调整） | 🟡 |
| 「网页压缩」1080p 预设 | 需新增 1080p 缩放预设（当前只有 `scale=-2:720`） | 🟡 |
| 「社交媒体」竖屏 9:16 | 需新增竖屏预设（`scale` + `pad`/`crop` 规则） | 🟡 |
| 「提取音频」MP3 320kbps | `extract-audio` 已存在，但 `PresetKind.AudioOnly` 无内置预设 | 🟡 |
| 「转 GIF」 | `Core` 无 GIF 预设、无调色板（`palettegen`/`paletteuse`）链路 | 🟡 |
| 「输出格式」H.264 / H.265 / WebM-VP9 | H.264 / H.265 有；WebM / VP9 无预设 | 🟡 |
| 「画质与体积」三档 | 部分可映射到现有 CRF，但非统一模型 | 🟡 |
| 暂停 / 继续任务 | `Core.Execution` **不支持暂停**，只有取消 | 🟡 |
| 队列进度百分比（68% / 24%） | 无解析：`ProgressBar` 仅 `IsIndeterminate` | 🟡 |
| 队列 Tab 计数（全部 7 / 处理中 2 …） | 按钮存在但**未接线**，计数为硬编码 | 🟡 |
| 任务行「更多操作」菜单 | 当前 `.row-menu` 直接切换状态，无菜单 | 🟡 |
| 通知中心 | 数据源应是任务状态变更 + `ExecutionResult`，当前无此模型 | 🟡 |
| 预设选择按任务隔离 | 当前 `PresetComboBox` 是**全局单例**，不是每任务 | 🟡 |
| 素材库导航 | 需要持久化媒体索引，当前不存在 | 🟡 |
| 设置页导航 | 需要持久化用户设置（`config` owner 仍为 `TBD`） | 🟡 |
| 搜索 | 无索引后端 | 🟡 |
| GPU 加速开关（`nvenc` 等） | **`Core` 无硬件编码器支持** | 🟡 |
| 本地空间 / 剩余容量条 | 原型为 `42.6 GB / 100 GB` 配额模型，本地无配额语义 | 🔴 |
| 用户账号 / `Pro workspace` | 本产品为本地工具，无账号体系 | 🔴 |
| 浅色 / 深色主题 | 当前仅浅色；纯 UI，无边界问题（但见 §5.1） | 🟢 |
| 「今日已处理 128 / 节省 4.8 GB / 本月用时」 | 需要本地统计持久化，且**节省空间需基于前后文件体积差** | 🟡 |
| 「↑18% 较昨日」趋势 | 需要按日聚合的历史统计 | 🟡 |

## 4. 需要补齐的后端能力（按 owner 分级）

### 4.1 `Core.Presets` — 预设面偏窄（阻塞原型「快速预设」）

原型展示了 4 张预设卡片，而当前内置预设与原型语义**对不上**：

- 缺少 1080p 缩放（`scale=-2:1080`）
- 缺少竖屏 9:16（`scale=-2:1920` + `pad` 或 `crop`）
- 缺少音频专用预设（`PresetKind.AudioOnly` 枚举已存在，但 catalog 里**一个都没有**）
- 缺少 WebM / VP9 预设
- 缺少 GIF 预设（需 `palettegen` + `paletteuse` 两段 filter，属于新命令构建路径）

> 注意：`PresetKind.AudioOnly` 和 `PresetKind.Remux` 已在模型中定义但无内置实例。补预设时必须同时补 `src/OpenVideoToolbox.Core.Tests/BuiltInPresetCatalogTests.cs` 与 `src/OpenVideoToolbox.Cli.Tests/snapshots/presets.json` 契约快照。

### 4.2 `Core.Execution` — 硬件加速缺失（阻塞原型「GPU 加速」）

`VideoEncoderSettings.Encoder` 是自由字符串，模型层**不需要改**；但：

- 5 个预设全部为 `libx264` / `libx265` / `copy`
- `Core` 中无 `nvenc` / `qsv` / `videotoolbox` 探测或回退逻辑
- 原型底部「GPU 加速 已启用」与通知「性能提升 2.4×」**当前完全是假数据**

若要真实支持，需要：可用性探测（`ffmpeg -encoders` 或试编码）→ 显式预设 → 不可用时明确回退，而不是静默改参数。这属于新能力，不能靠 UI 开关实现。

### 4.3 `Core.Execution` — 无进度上报（阻塞原型进度条）

`DefaultProcessRunner` 采集完整 `stdout` / `stderr`，但**没有**解析 `ffmpeg` 的 `-progress` 或 `time=` 输出。原型展示的 `处理中 68%` 必须由 `Core.Execution` 提供结构化进度事件；`Desktop` 不得自己解析 `ffmpeg` 输出（违反 `ARCHITECTURE_GUARDRAILS.md` 的 Forbidden Ownership）。

### 4.4 `Core.Execution` — 无暂停 / 继续（阻塞原型「已暂停」）

当前只有 `CancellationToken`（取消）。进程级暂停需要额外的信号或重启语义。原型的 3 个状态（处理中 / 已完成 / 已暂停）与 `WorkbenchTaskState` 的 5 个状态（Ready / Running / Succeeded / Failed / Cancelled）**语义不一致**，需要先定状态映射再改 UI。

### 4.5 持久化 — 统计与素材库（`config` / `persistence` owner 仍为 `TBD`）

`ARCHITECTURE_GUARDRAILS.md` 明确：

- `config`：跨应用持久化配置 owner = `TBD`
- `persistence`：当前仓库未建立持久化层，owner = `TBD`

原型的素材库、设置页、历史统计、通知中心都需要持久化。**在 owner 未确认前不应实现**，否则会形成第二个持久化 owner。

## 5. Desktop 现有实现的 3 个正确性问题

这三项与原型无关，但会直接影响"能否作为可发布设计"：

> **状态更新（2026-10-11）：以下 3 项已全部修复。** 修复内容见本节各条末尾的「已修复」说明；验证方式为 `dotnet build` + `dotnet test`（`396 passed`）及 Desktop 启动 smoke。

### 5.1 原型的深色主题与已冻结的浅色设计冲突

- `docs/plans/desktop/2026-10-11-desktop-workbench-design.md` §3 明确冻结：「工作台采用浅灰背景、白色面板」「深色区域只用于日志内容，不作为主要导航或任务编辑背景」
- commit `fda1e1f refactor(desktop): 重构工作台布局与视觉层级` 已按此落地
- 原型是反过来的：深色为主，只有日志面板深色相同

这是**产品边界级的视觉决策**，不是纯样式问题，需要明确二选一后再改。

**已修复**：经确认改为**浅色 / 深色双主题**。调色板分列 `src/OpenVideoToolbox.Desktop/Themes/Light.axaml` 与 `Dark.axaml`，两份字典声明同一组 `App*` 键；`ThemeManager` 负责合并字典交换，`WorkbenchPreferencesStore` 负责持久化到 `desktop-preferences.json`；主题在 `App.OnFrameworkInitializationCompleted` 中于窗口创建前应用，避免启动闪白。`MainWindow.axaml` 中的颜色字面量已全部替换为主题资源键。`design.md` §3 的视觉冻结条款已同步解除并改写。

### 5.2 计划 Tab 硬编码模板，违反 `edit.json` 单一计划边界

`OnExportPlanClick`（原 `MainWindow.axaml.cs:394`）硬编码：

```csharp
new EditPlanTemplateFactory().Create("shorts-captioned", ...)
```

问题：

1. 用户选了 `h265-aac-mp4` 等其他预设，导出的仍是 `shorts-captioned` 计划，与实际执行的任务不一致；
2. `Desktop.MODULE.md` 的不变量要求 Desktop 把 `edit.json` 视为唯一计划边界，不得发明 UI 私有计划格式——硬编码模板属于绕过模板发现；
3. 正确做法应是消费 `templates` 发现结果（`BuiltInEditPlanTemplateCatalog` / `EditPlanTemplateCatalogQuery`），让用户显式选模板，而不是 UI 猜一个。

**已修复**：「计划」Tab 新增 `PlanTemplateComboBox`，数据源为 `BuiltInEditPlanTemplateCatalog.GetAll()`；`OnExportPlanClick` 改为使用显式选中的模板，并把选择持久化到 `WorkbenchTask.PlanTemplateId`（随后续任务恢复）。未选择模板时明确报错而不是回退到猜测值。

### 5.3 预设与输出目录按任务隔离不完整

- `PresetComboBox` / `OutputDirectoryTextBox` / `OverwriteComboBox` 是**窗口级单例控件**
- `OnAddVideosClick` 添加文件时用的是 `BuiltInPresetCatalog.DefaultPresetId` 和当前输出目录，但后续切换选中任务才把控件值写回任务（`TryBuildJob`）
- 结果：在任务 A 上改了输出目录再切到任务 B，B 的控件值被 A 覆盖；`OnRunQueueClick` 批量执行时用的是**任务自己的** `PresetId`，与 UI 显示可能不一致

原型的「快速预设 + 每行任务」交互天然要求按任务隔离，这项必须先修。

**已修复**：`TryBuildJob` 引入 `_settingsBoundTask` 写回保护——只有当前已装载控件值的任务可以回写，其余任务一律使用自身已保存的 `PresetId`；新建队列项统一走 `CreateQueueItem`，会采用当前选中的预设与模板而不是无条件使用默认值；`RebuildVisibleQueue` 在被绑定任务离开可见集合时解除绑定，避免回写指向隐藏任务。


## 6. 建议对齐顺序

不建议直接按原型逐块实现，建议分三步：

### 第 1 步：先修正确性（不扩能力）— ✅ 已完成（2026-10-11）

1. ✅ 修 `OnExportPlanClick` 硬编码模板 → 改为显式模板选择
2. ✅ 修预设 / 输出目录按任务隔离
3. ✅ 接线队列 Tab 筛选与计数（按钮已在，只需绑定）
4. ✅ 浅色 / 深色双主题，并解除 `design.md` 的单主题冻结

### 第 2 步：再把原型做成能消费真实能力的壳

优先接已实现的 Core 能力，让界面不出现假数据：

1. 媒体信息行 → `MediaProbeResult` 完整字段
2. 预设卡片 → 先只映射现有 5 个预设，命名与原型语义对齐
3. 通知中心 → 由 `WorkbenchTask` 状态变更 + `ExecutionResult` 派生
4. 底部状态栏 → `ExternalDependencyInspector` 完整结果

### 第 3 步：再决定是否补后端能力

需要**显式决策**才能进入：

| 能力 | 需确认 |
| --- | --- |
| GPU 硬件加速 | 是否纳入产品范围；若纳入，owner 是 `Core.Presets` + `Core.Execution` |
| 进度百分比 | 是否给 `Core.Execution` 加结构化进度事件 |
| 暂停 / 继续 | 是否改变 `WorkbenchTaskState` 并引入进程暂停语义 |
| 素材库 / 设置 / 统计 | 先确认 `persistence` 与 `config` 的 canonical owner |
| GIF / 竖屏 / 音频预设 | 属低风险扩展，但需同步预设快照测试 |
| 云空间 / 账号 / `Pro` | 建议从原型移除，与本产品本地工具定位不符 |

## 7. 发布功能完备度检查清单

### 7.1 CLI / Core（`v1.0.0` 正式支持面）

| 项 | 状态 |
| --- | --- |
| 主工作流全部跑通 | ✅ 已完成（见预检卡 release gate G1–G3） |
| 契约快照与 `--json-out` 稳定 | ✅ |
| 发布资产跨平台产出 | ✅ |
| release note 成稿 | ⚠️ 仍是草案 |
| `go / no-go` 正式签发 | ❌ 未完成 |
| `v1.0.0` tag 与 GitHub Release | ❌ 未创建 |

**结论：CLI 侧的"功能完备"已经达标，卡住发布的是纪律动作，不是功能。**

### 7.2 Desktop（不在 `v1.0.0` 支持面）

| 项 | 状态 |
| --- | --- |
| D1-0 ~ D1-4 阶段验收 | ✅ 已完成 |
| `win-x64` publish smoke | ✅ 已完成 |
| UI 关键交互自动化测试 | ❌ 未落地（`PROJECT_PROFILE.md` 已登记为待补；Desktop 仍无独立测试项目） |
| 原型对齐的视觉决策 | ✅ 已决策为浅色 / 深色双主题并实现 |
| 计划边界正确性（硬编码模板） | ✅ 已修复 |
| 队列筛选与计数 | ✅ 已接线 |
| 预设 / 输出目录按任务隔离 | ✅ 已修复 |
| 后端能力（GPU / 进度 / 暂停 / 持久化） | ❌ 均未实现（属第 3 步，需先确认 owner） |

## 8. 文档同步事项

- `docs/PROJECT_PROFILE.md` 中 Desktop 状态描述为「Avalonia 工作台已接入…D1-4 已完成 Windows `win-x64` publish smoke」，但缺少 2026-10-11 D1 工作面与原型探索的记录 —— 已在本次同步更新。
- 若后续确认了 GPU 加速 / 进度上报 / 暂停继续 / 持久化 owner，必须更新：
  - `docs/ARCHITECTURE_GUARDRAILS.md`（`config`、`persistence` owner 从 `TBD` 落定）
  - `src/OpenVideoToolbox.Desktop/MODULE.md`
  - `docs/roadmap.md`（新阶段卡）
- 本文不改变 `D1-4 收口` 结论：Desktop 仍不做发布版本复核以外的新增业务能力。

## 9. 边界声明

- 本文不主张把 `Desktop` 纳入 `v1.0.0`。
- 本文不主张实现原型中的云空间、账号、`Pro workspace` 等超出本地工具定位的模块。
- 本文提出的所有后端能力（GPU、进度、暂停、持久化）都**需要先确认 owner**，不得由 `Desktop` 自行实现。
