# v1.0.0 Go / No-Go 签发稿（草案）

> 历史签发草稿：本文保留 2026-04-26 的 `v0.1.0` / `v1.0.0` 对照证据。当前项目版本已调整为 `1.0.0`，最新发布验证以 [2026-10-13 v1.0 技术债与发布验证卡](2026-10-13-v1-0-technical-debt-and-release-verification-card.md) 为准。

最后更新：2026-04-26

## 目的

这份文档用于在真正创建 `v1.0.0` tag 前，给 maintainer 一张可直接执行、可直接填写结论的最终签发表。

它只回答四件事：

1. 本次准备签发的正式支持面是什么
2. 当前已经有哪些可复核证据
3. 哪些问题会阻塞签发，哪些问题只能带说明发布
4. 最终结论是 `Go`、`Conditional Go` 还是 `No-Go`

它不替代完整边界讨论；详细背景仍以 `docs/plans/v1.0/2026-04-26-v1-0-release-precheck-and-boundary.md` 为准。

## 适用版本

- 目标版本：`v1.0.0`
- 当前公开版本基线：`v0.1.0`
- 当前文档状态：签发草案，未实际发布 `1.0.0`

## 正式支持面

本次 `v1.0.0` 只按 `CLI + Core` 交付面签发，不把 `Desktop` 纳入正式支持面。

### 主承诺能力

- `doctor`
- `probe` / `plan` / `run`
- `cut` / `concat` / `extract-audio`
- `audio-analyze` / `audio-gain` / `audio-normalize`
- `templates` / `init-plan` / `scaffold-template` / `validate-plan` / `render`
- `inspect-plan` / `replace-plan-material` / `attach-plan-material` / `bind-voice-track`
- `scaffold-template-batch` / `render-batch`
- `mix-audio --preview` / `mix-audio`
- 跨平台 single-file release 资产
- 稳定结构化 envelope 与 `--json-out`

### 附带但不作为主阻塞项的能力

- `init-narrated-plan` / `init-narrated-plan-batch`
- `effects`（当前范围仅 `move / orbit / zoompan`）
- `export --format edl`
- `schemaVersion = 2`
- `transcribe`
- `separate-audio`

## 已知限制

- `Desktop` 当前不在 `v1.0.0` 正式支持面。
- `valid draft != directly renderable output`；裸 `init-plan` / `scaffold-template` 可能产出合法但 `clips = []` 的草稿。
- `subtitles.mode = sidecar` 当前语义是复制同名 `.srt` 到输出旁路，不是字幕烧录。
- `transcribe` / `separate-audio` 依赖 optional tooling，不应视为默认环境保证。
- 当前 `effects` 不扩展为角色动画、`Live2D`、骨骼绑定或关键帧工作台。

## 签发前检查表

| 类别 | 条目 | 当前基线 | 结果 | 阻塞级别 | 备注 |
| --- | --- | --- | --- | --- | --- |
| 版本 | 公开版本与目标版本区分清晰 | `v0.1.0` 已发布，`v1.0.0` 尚未发布 | Pass | 阻塞 | 当前文档口径已区分“已发布”与“待签发” |
| 资产 | GitHub Release 形态已验证 | `v0.1.0` latest release 存在，已确认 `ovt-win-x64.exe`、`ovt-win-x64.zip`、`ovt-linux-x64.tar.gz`、`ovt-osx-x64.tar.gz` | Pass | 阻塞 | 需在真正发版前按同口径复核 `v1.0.0` tag 资产 |
| 启动 | 干净目录启动样本 | fresh `ovt.exe --help`、`doctor --json-out` 已通过 | Pass | 阻塞 | stdout 与文件输出一致 |
| 依赖 | `doctor` required / optional 区分稳定 | `isHealthy = true`、`missingRequiredCount = 0`、`missingOptionalCount = 3` | Pass | 阻塞 | optional 缺失集中在 `whisper-cli` / `demucs` / `whisper-model` |
| 构建 | 解决方案可构建 | `dotnet build OpenVideoToolbox.sln` 已通过 | Pass | 阻塞 | 证据见预检卡 |
| 测试 | 全量自动化通过 | `388 passed` | Pass | 阻塞 | `Core.Tests 193` + `Cli.Tests 195` |
| 契约 | JSON envelope / `--json-out` 稳定 | 已有 contract snapshot 与 CLI tests | Pass | 阻塞 | 当前未发现 breaking drift 证据 |
| 主工作流 | `probe -> plan -> run` | 发布物视角 smoke 已通过 | Pass | 阻塞 | 已产出 `smoke-output.mp4` |
| 主工作流 | `templates -> init-plan -> validate-plan -> render` | seeded plan 路径已通过 | Pass | 阻塞 | bare draft 需在 release note 明确限制 |
| 主工作流 | `scaffold-template -> inspect / attach / replace / render` | fresh `win-x64` single-file `ovt.exe` release-view smoke 已复跑 | Pass | 阻塞 | **2026-04-26** 已复跑 transcript-seeded `shorts-captioned` 维护链：`inspect-plan`、`attach-plan-material`、`replace-plan-material`、`bind-voice-track`、`render` 均通过；`final.mp4` 与 sidecar `final.srt` 已产出 |
| 主工作流 | `scaffold-template-batch -> render-batch` | fresh `win-x64` single-file `ovt.exe` release-view smoke 已复跑 | Pass | 阻塞 | **2026-04-26** 已复跑：`scaffold-template-batch` 成功写出 batch `summary.json` / `results/<id>.json`；`render-batch --preview` 成功消费 batch workdir 下 plan，并返回 `2/2` succeeded |
| 非主阻塞 | `transcribe` / `separate-audio` | 默认环境缺 optional dependency | Accepted gap | 非阻塞 | 只要 release note 明确即可 |
| 非主阻塞 | `init-narrated-plan` / `effects` / `export` | 已有阶段验收材料 | Accepted gap | 非阻塞 | 不作为 `v1.0.0` 主放行项 |

## 阻塞规则

以下任一情况出现，结论必须为 `No-Go`：

1. 任一主承诺工作流在受支持环境中无法重复跑通。
2. 关键命令成功或失败路径不再输出稳定结构化 envelope。
3. `--json-out` 与 stdout 语义出现漂移。
4. `v1.0.0` 发布资产缺失、不可运行，或与文档口径不一致。
5. release note 把非正式支持面误写成正式承诺。

以下问题默认可接受，但必须在 release note 中明确写出：

1. optional dependency commands 在默认环境不可用。
2. bare draft 需要后续 seed 或人工编辑后才可渲染。
3. `effects`、`narrated`、`export`、`schemaVersion = 2` 仍属受限能力。
4. `Desktop` 仍未进入正式支持面。

## 建议签发结论模板

### 执行记录

| 字段 | 填写内容 |
| --- | --- |
| 执行日期 |  |
| 执行人 |  |
| 目标 tag | `v1.0.0` |
| 依据文档 | `docs/plans/v1.0/2026-04-26-v1-0-release-precheck-and-boundary.md` |
| release note 基线 | `CHANGELOG.md` |
| 最终 smoke 入口 | `.artifacts/v1-signoff-smoke-r2/summary.json` |
| 阻塞缺陷 | 待填写 |
| 非阻塞缺陷 | optional dependency commands 默认环境不可用；`effects` / `narrated` / `export` / `schemaVersion = 2` 仍按受限附带能力处理 |

### 最终结论

从 **2026-04-26** 当前证据看，更合理的状态仍不是直接写死“已正式发布”，但代码与发布物基线已经足够收敛到：

> `Conditional Go`

含义：

- 主支持面、契约稳定性、发布物启动样本、自动化测试基线以及最终 release-view smoke 已全部到位
- 当前剩余条件不再是“主工作流是否能跑通”，而是 maintainer 在真正发版动作中复核 `v1.0.0` tag、GitHub Release 资产与 release note 是否与本稿一致

### 可直接填写的结论区

```text
最终结论：Go / Conditional Go / No-Go
执行日期：
执行人：
阻塞项是否已清零：Yes / No
是否允许创建 v1.0.0 tag：Yes / No
是否允许发布 GitHub Release：Yes / No
需随 release note 同步披露的限制：
后续第一优先修复项：
```

## 建议实施顺序

1. 复核 `CHANGELOG.md` 与本稿口径一致。
2. 复核 `.artifacts/v1-signoff-smoke-r2/summary.json` 与本稿记录一致；如 maintainer 需要，可自行再抽样复跑。
3. 在真正发版前复核 `v1.0.0` tag、GitHub Release 资产名与 release note 文案是否一致。
4. 若无阻塞缺陷，则将最终结论从 `Conditional Go` 改为 `Go`。
5. 再创建 `v1.0.0` tag 与 GitHub Release。

## stop-point

本稿完成后，不在同一轮继续扩成功能规划或新版边界讨论；下一动作应是执行签发，而不是再写新的范围卡。
