# Roadmap

> 最后更新：2026-10-14

本文件只维护当前产品目标、活跃工作面、优先顺序和阶段入口。历史阶段结论与验收细节留在专项卡片中；本文件不作为执行流水账。

## 产品目标与正式范围

v1.0 的正式交付面是 **Core + CLI**，目标用户兼顾压缩分享与内容生产。重点是可复用、可审计、可批量运行的本地媒体工作流；AI 通过外部 CLI 编排，不在软件内集成远程模型。

Desktop 在 `ui` 分支作为独立工作线保留，不属于 v1.0 发布资产或放行条件。v2 时间线能力在仓库中已有实现，但不改变当前 v1.0 的发布边界，也不作为本轮默认扩展方向。

## 当前活跃事项

| 顺序 | 工作面 | 状态 | 当前动作 |
| --- | --- | --- | --- |
| 1 | 文档分类与导航 | 已完成 | 规则和入口见 [文档治理指南](DOCUMENTATION_GUIDE.md)、[文档索引](README.md) 与 [计划导航](plans/README.md)。 |
| 2 | 整体架构 | 已完成 | 当前模块、依赖和数据流见 [架构文档](architecture.md)。 |
| 3 | Roadmap 收敛 | 已完成 | 本页只保留当前状态与专项入口。 |
| 4 | v1.0 技术债与发布验证 | 已完成（Conditional Go） | V1-V8 证据、blocker 分级和 fresh single-file smoke 已记录；实际 `v1.0.0` tag / GitHub Release 仍是 maintainer 的独立签发动作。 |
| 5 | 压缩结果变大保护 | 首版已完成 | `run` 默认拒绝更大结果并保护旧输出；`--allow-larger-output` 作为显式 override，真实素材验证记录在技术债与发布验证卡。 |
| 6 | 统一尺寸与画幅 | 首版已完成 | `plan` / `run` 支持 `--resolution` 与 `--fit`，Core 统一生成 `scale + pad` 滤镜；详见功能规划卡。 |
| 7 | 抽帧与缩略图 | 首版已完成 | `extract-frame` 支持时间点和固定间隔批量输出，默认同目录后缀命名且不覆盖输入；已纳入 V1-V8 验证证据。 |
| 8 | 水印 | 首版已完成 | `plan` / `run` 支持图片、九宫格位置、边距和透明度；命令构建和失败边界已测试。 |
| 9 | 字幕样式 | 首版已完成 | `plan` / `run` 的烧录字幕支持受限 `force_style` 字段；样式模型和快照已同步。 |
| 10 | doctor 硬件编码器探测 | 首版已完成 | `doctor` 报告 `hardwareEncoders`，不自动选择或回退；显式试编码会保留驱动错误上下文。 |
| 11 | v1.0 发布验证 | 本地候选通过（Conditional Go） | 本地 `main` 候选 V1-V8 与 Windows single-file smoke 已通过；当前领先 `origin/main` 67 个提交。签发前先确认并同步候选，再由 tag workflow 检查三平台资产；证据见 [技术债与发布验证卡](plans/v1.0/2026-10-13-v1-0-technical-debt-and-release-verification-card.md)。 |
| 12 | v1.1 目录批量压缩与安全替换 | 已规划 | 详见 [v1 到后续版本功能规划卡](plans/v1.0/2026-10-13-v1-feature-roadmap-and-capability-card.md)；当前仍使用单文件 `run`，未承诺递归遍历或原地替换。 |

问题列表和逐项进展见 [当前问题记录](analysis/issue/current/261013问题.md)。

## v1.0 CLI 主线

本地 `main` 候选的 V1-V8 验证和 v1.0 尾部能力均已完成，项目版本为 `1.0.0`；当前本地分支领先 `origin/main` 67 个提交，尚未形成远端发布候选。下一步先确认并同步这批提交，再复核 `CHANGELOG.md`，由 `v1.0.0` tag workflow 构建和检查三平台资产；是否正式签发由 maintainer 最后确认。已交付命令、功能矩阵和 v1.0 发布门槛以 [v1.0 到后续版本功能规划卡](plans/v1.0/2026-10-13-v1-feature-roadmap-and-capability-card.md) 为准。

后续功能候选按该卡的 v1.1 区域推进：优先设计目录批量压缩与安全替换，再依据 `doctor` 探测结果评估硬件编码预设，并评估两遍目标大小、任务恢复和平台预设包。复杂时间线、专业调色等不纳入 v1.0 默认承诺。每项进入实现前仍需明确 owner、兼容性、失败行为和验收标准。

### v1.0 放行原则

- 只有发布边界、核心工作流验证和发布资产复核完成后，才能签发 v1.0；日期计划和候选功能列表本身不代表放行。
- 验证覆盖压缩、模板生成与渲染、素材绑定、批量结果、依赖诊断及结构化输出。
- 对真实媒体样本记录输入/输出大小、命令结果和异常行为；压缩结果变大保护由上方第 5 项单独收敛。
- 发布前刷新既有预检与签发材料，避免沿用过期的历史 smoke 结果；本轮证据见 [技术债与发布验证卡](plans/v1.0/2026-10-13-v1-0-technical-debt-and-release-verification-card.md)。

发布边界与候选功能详见 [功能规划卡](plans/v1.0/2026-10-13-v1-feature-roadmap-and-capability-card.md)、[发布预检](plans/v1.0/2026-04-26-v1-0-release-precheck-and-boundary.md) 和 [发布签发稿](plans/v1.0/2026-04-26-v1-0-go-no-go-signoff-draft.md)。

## 独立工作线

### Desktop

Desktop 保留在 `ui` 工作线上，复用 Core 能力；其工作范围、阶段和手动验收见 [Desktop 工作台计划](plans/desktop/2026-10-11-desktop-workbench-implementation-plan.md) 及 [阶段检查](plans/desktop/2026-10-11-desktop-workbench-phase-check.md)。它不改变 v1.0 Core + CLI 的发布门槛。

### v2 孵化

v2 的 timeline、模板 effects 与 narrated-slides 等现有能力仍按各自计划维护。当前没有从 v1.0 发布线自动进入 v2 实施的安排；任何扩展需先有独立的明确计划和阶段验收。入口见 [v1/v2 边界与阶段计划](plans/v2/2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md) 与 [v2 计划索引](plans/README.md#v2-孵化与后续版本)。

## 维护规则

- 当前状态只在本文件维护一次；专项计划记录范围、决策、验证证据和历史结论。
- 完成一项后更新状态和对应卡片链接，删去不再活跃的流水细节。
- 发生产品边界、模块 owner 或验收标准变化时，同步 PRD、架构护栏或模块文档。
