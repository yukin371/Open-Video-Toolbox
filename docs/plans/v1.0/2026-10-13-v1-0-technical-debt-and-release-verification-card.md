# v1.0 技术债扫描与发布验证卡

> 状态：complete（证据包完成；实际 tag/release 仍待 maintainer 执行）
> owner：维护者；Core / Cli 模块 owner 对各自条目提供证据
> 创建日期：2026-10-13
> 目标版本：v1.0.0

## 目标

在创建 `v1.0.0` tag 前，形成一份可复核的技术债与发布证据包，回答：

1. 正式承诺的 Core + CLI 工作流是否能在受支持环境中重复运行。
2. 已知技术债是否会破坏数据安全、命令契约、错误可见性或发布资产。
3. 非主线能力、可选依赖和 Desktop / v2 能力是否都已明确标注边界。
4. 发布判断能否由命令、样本和结构化产物复现，而不是依赖口头结论。

本卡不扩大 v1.0 功能范围，也不把 Desktop 纳入发布阻塞条件。

## 扫描范围

### 技术债类别

| 类别 | 重点检查 | 默认阻塞条件 |
| --- | --- | --- |
| 契约 | JSON envelope、snapshot、`--json-out`、退出码、schemaVersion | 已发布或正式承诺的命令发生未记录 breaking change |
| 执行安全 | 外部进程日志、stderr、超时、取消、输出路径、overwrite | 错误被吞掉、源文件被覆盖、旧产物被误删 |
| 产物一致性 | `ProducedPaths`、sidecar、batch summary/result、失败清理 | 命令成功但产物不可定位，或失败留下会被误认为成功的文件 |
| 依赖与发布 | `doctor`、ffmpeg/ffprobe、single-file 发布物、版本号 | required 依赖无法诊断，发布物无法启动或版本不一致 |
| 工作流闭环 | `probe -> plan -> run`、模板、plan 维护、batch | v1 主工作流无法按文档复现 |
| 文档与边界 | README、Roadmap、发布预检、Desktop/v2 限定 | 文档把非承诺能力宣传成稳定支持面 |
| 测试维护 | Core / CLI 测试、快照、real-media smoke | 关键路径没有自动化或可复现的最小 smoke |

### 不在本卡内

- Desktop 新功能开发或 UI 体验扩展。
- v2 timeline、effects、narrated 的新范围。
- 新增外部运行时依赖、数据库或跨进程调度层。
- 以“文件变小”推断画质提升；压缩大小只作为可见诊断信号。

## v1.0 正式验证矩阵

| 编号 | 工作流 | 验证入口 | 通过标准 | 证据 |
| --- | --- | --- | --- | --- |
| V1 | 依赖与启动 | `dotnet build .\OpenVideoToolbox.sln`；`dotnet run --project .\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- doctor` | build 成功；required 依赖状态明确；optional 缺失不伪装成核心故障 | 已通过：doctor required=0 missing，optional=3 |
| V2 | 单素材压缩 | `probe -> plan -> run` | 输出位于预期路径，返回结构化执行结果和输入/输出大小对比；更大结果默认拒绝并不提交 | 已通过：全量测试 + Captures 真实 smoke |
| V3 | 模板草稿与渲染 | `templates -> init-plan/scaffold-template -> validate-plan -> manual or seeded clips -> render` | 空草稿语义有文档说明；seeded 或人工 clips 能 preview 和执行 | 已通过：`templates --summary`、`init-plan --probe` 和真实 Captures `scaffold-template --probe --validate --check-files` 均完成；`render --preview` 消费带 `clip-001` 的 plan |
| V4 | Plan 维护 | `inspect-plan -> attach/replace/bind-voice-track -> validate/render` | 绑定和替换结果可复查，失败保留错误上下文 | 已通过：先 `inspect-plan --check-files`，再依次完成 transcript attach、voice bind、audio replace；`validate-plan --check-files` 和 `render --preview` 均通过 |
| V5 | 批量 | `scaffold-template-batch -> render-batch --preview` | 写出 `summary.json` 与 `results/<id>.json`；部分失败退出码稳定 | 已通过：`scaffold-template-batch` 两项成功并写出 batch artifacts；`render-batch --preview` 另以一成功一失败 manifest 复现退出码 `2`，并写出根目录 `summary.json` 与两个 `results/<id>.json` |
| V6 | JSON 契约 | `dotnet test .\OpenVideoToolbox.sln --no-restore` | Core / CLI 测试、快照和失败路径通过 | 已通过：2026-10-14，231 Core + 211 CLI passed，0 skipped；尺寸、抽帧、字幕样式和 subtitle 成功/失败 envelope 契约已同步 |
| V7 | 发布物 | Release workflow + fresh publish smoke | single-file 可启动；`--help`、`doctor` 与版本信息一致 | 已通过：2026-10-14 fresh `win-x64` single-file publish；文件版本 `1.0.0.0`、产品版本 `1.0.0+5d6fe48e...` 与 `VersionPrefix 1.0.0` 一致，`ovt.exe --help` 和 `doctor --json-out` 均退出 `0`；证据在 `.artifacts/v1-signoff-smoke-20261014-r3/`；`v1.0.0` tag / Release 尚未签发 |
| V8 | 可选能力 | `transcribe`、`separate-audio`、narrated、effects、export | 依赖和受限范围在文档中清晰；不可用时错误可解释 | 已通过：`effects list` 和 `export --format edl` 通过；缺少 `whisper-cli` / `demucs` 时 `transcribe` / `separate-audio` 返回结构化错误 envelope；受限范围见发布边界文档 |

## 技术债登记

每项债务记录以下字段，未完成时不要从 Roadmap 删除：

| ID | 类别 | 描述 | owner | 级别 | 处理或接受条件 | 证据 / 链接 |
| --- | --- | --- | --- | --- | --- | --- |
| TD-001 | 产物一致性 | 历史实现曾在转码完成后直接提交较大结果 | Core.Execution + Cli | resolved | 已改为临时输出；默认拒绝较大结果，`--allow-larger-output` 显式覆盖；已补源文件、旧输出、sidecar 和失败清理测试 | [当前问题](../../analysis/issue/current/261013问题.md) |
| TD-002 | 发布证据 | 历史 smoke 结果分散在旧计划卡，缺少本版本一页式证据包 | Maintainer | non-blocking | 按本卡 V1-V8 补齐结果、日期、环境和产物路径 | 本卡 |
| TD-003 | 可选依赖 | whisper-cli、模型和 demucs 不在默认环境中 | Core.Execution + Cli | accepted risk | `doctor` 明确 required/optional；发布文档不把它们当作 v1 主路径 | [依赖说明](../../external-dependencies.md) |
| TD-004 | Desktop 边界 | `ui` 分支已有未提交工作，不属于 v1.0 发布资产 | Desktop owner | accepted risk | 保留独立分支和文档入口；v1 发布只从 Core + CLI 证据判断 | [Roadmap](../../roadmap.md) |

级别含义：

- `blocker`：必须处理后才能创建 tag。
- `blocker candidate`：先完成最小复现和影响确认；若会造成用户误用或数据丢失，升级为 blocker。
- `non-blocking`：不影响主工作流，但要有明确 owner 和后续入口。
- `accepted risk`：在发布说明中明确限制，后续版本再评估。

## 执行记录模板

每条验证至少记录：

```text
编号：
执行日期：
提交 / 分支：
环境：
命令：
输入样本：
预期：
实际：
产物：
stdout / stderr / JSON 证据：
结果：pass / fail / blocked
缺陷关联：
```

真实媒体 smoke 应记录源文件大小、输出文件大小、deltaBytes、ratio、输出路径和是否覆盖已有文件。不要只保留截图或人工描述。

### Captures 真实素材记录（2026-10-13）

执行目录：`C:\Users\yukin\Videos\Captures\ovt-v1-smoke-20261013`。源目录未被写入，报告汇总见该目录的 `summary.json`。

- 5 个视频均完成 `run`；4 个结果变小并提交，1 个已压缩素材再次转码后变大 47.34%，默认返回退出码 2 且未提交正式输出。
- 对同一已压缩素材使用 `--allow-larger-output` 后，结果以退出码 0 提交；报告保留 `isLarger = true` 和完整大小对比。
- 已验证原始文件、已有输出、临时产物清理和默认同目录命名行为。

### v1.0 尾部能力记录（2026-10-14）

- `--resolution` / `--fit`：`VideoSizingSettings` 统一承接 480p、720p、1080p、显式尺寸及 9:16 / 1:1 / 16:9 补边；Core 命令构建、CLI 集成和 `presets` 契约快照通过。
- `extract-frame`：支持 `--at` 多点与 `--start` / `--interval` / `--count` 批量抽帧；默认输出输入同目录的 `_frame-001.png` 系列，禁止覆盖输入；Core、CLI fake executable 和真实媒体 smoke 测试通过。
- 本轮全量验证：`dotnet test .\OpenVideoToolbox.sln --no-restore`，231 Core + 211 CLI passed，0 failed，0 skipped；修正 `subtitle` 成功与异常路径统一 envelope，并覆盖 `--json-out` 一致性。

### 发布签发 smoke 记录（2026-10-14）

执行分支：`ui`；环境：Windows、`.NET 8`、`ffmpeg 8.1-essentials_build-www.gyan.dev`；真实输入取自 `C:\Users\yukin\Videos\Captures\剪映专业版 2026-01-24 13-35-09.mp4`。完整 JSON、stdout/stderr 和发布物位于本地忽略目录 `.artifacts/v1-signoff-smoke-20261014-r1/`。

| 编号 | 命令与证据 | 实际结果 |
| --- | --- | --- |
| V3 | `scaffold-template <capture> --template shorts-captioned --dir <workdir> --probe --validate --check-files`；随后 `render --plan <workdir>/edit.json --preview` | `probed=true`、`validated=true`、`isValid=true`；探测生成 `clip-001`（完整源片段），render preview 生成稳定 FFmpeg command plan，退出码 `0` |
| V4 | `attach-plan-material --transcript` -> `bind-voice-track` -> `replace-plan-material --audio-track-id voice-main` -> `validate-plan --check-files` -> `render --preview` | transcript、voice-main 和替换后的 `audio/dub-new.wav` 均出现在 plan；每一步校验 `isValid=true`，render preview 退出码 `0` |
| V5 | `render-batch --manifest render-batch.json --preview`，manifest 含一个有效 plan 和一个缺失 plan | 写出 `summary.json`、`results/job-ok.json`、`results/job-missing.json`；`itemCount=2`、`succeededCount=1`、`failedCount=1`，退出码 `2`，失败结果保留路径错误上下文 |
| V7 | `dotnet build .\OpenVideoToolbox.sln --no-restore`；`dotnet publish ... -r win-x64 --self-contained true -p:PublishSingleFile=true`；从发布目录执行 `ovt.exe --help` 与 `ovt.exe doctor --json-out published-doctor.json` | build 0 warning / 0 error；生成 `ovt.exe`；help 和 doctor 均退出 `0`；doctor `isHealthy=true`、required 缺失 `0`、optional 缺失 `3`，并列出硬件编码器；文件版本 `1.0.0.0` 与 `VersionPrefix 1.0.0` 一致；证据目录 `.artifacts/v1-signoff-smoke-20261014-r3/` |
| V8 | `effects list`、`export --format edl`、缺少 optional 依赖时的 `transcribe` / `separate-audio` | effects（14 descriptors）和 EDL export 退出 `0`；缺少 `whisper-cli` / `demucs` 时均返回结构化 failure envelope、stderr 保留启动错误；不把 optional 缺失误报为 required 故障 |

V3 的空草稿限制仍然成立：未使用 `--probe`、transcript seed、beats seed 或人工 clips 时，`scaffold-template` 可以生成合法但 `clips=[]` 的 plan；该边界已写入发布签发稿，不视为隐藏失败。

## 发布签发门槛

以下条件必须同时满足：

- V1-V7 有可复核的 pass 证据；V8 的限制已在文档和 release note 中标明。
- 技术债登记中不存在未处理的 blocker。
- `run`、`render`、batch 和 sidecar 路径不会静默覆盖源文件或隐藏失败。
- 发布物版本号、程序集名、tag、workflow 资产和文档口径一致。
- Desktop 与 v2 能力未被误写成 v1.0 正式承诺。
- 任何“压缩后变大”的行为在发布前完成单独决策：默认拒绝/保留源文件，或明确记录为已知限制并降级为非阻塞。

## 本轮结论

```text
结论：Conditional Go
证据状态：V1-V8 已补齐，未发现未处理 blocker
当前程序集版本：1.0.0
v1.0.0 tag / GitHub Release：尚未执行
签发前剩余动作：刷新 V7 fresh publish 证据，maintainer 最后复核 release note、tag 与平台资产名称后再执行发布
```

这表示 Core + CLI 的主工作流和发布物验证已经达到签发条件；版本号已统一为 `1.0.0`，本卡仍不代替实际创建 tag 或发布 GitHub Release。

## 完成定义

本卡只有在执行记录补齐、技术债完成分级、阻塞项有处理结果、发布边界同步到 [Roadmap](../../roadmap.md) 和 [计划导航](../README.md) 后，才能标记为 `complete`。测试命令通过但证据未记录时，状态仍保持 `in progress`。
