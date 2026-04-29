# 外部视频与 AI 编排补充功能计划

最后更新：2026-04-29

## 目的

本计划参考 `youtube/` 目录下的 YouTube Clipper Skill，但不把它作为运行时子系统并入仓库。

目标是把其中有价值的产品能力转成 Open Video Toolbox 可维护的功能路线：

- 外部视频来源先落成本地素材，再进入 CLI 可审计流程
- 外部 AI 负责章节、翻译、文案等语义工作
- OVT 负责结构化文件、计划生成、素材接回、校验、预览和渲染
- 任何新能力都不突破现有 `Core / Cli / Desktop` owner 边界

## 当前结论

短期不新增下载器、不接 AI provider、不移植 Python 脚本。

当前最适合推进的是：

1. 先把外部视频 / 外部 AI 工作流文档化
2. 再定义 `chapters.json`、外部字幕、发布文案等工作目录产物约定
3. 最后评估哪些确定性能力值得变成 CLI 命令

## 与现有阶段的关系

这不是立即启动的新实现阶段，而是 `E2` 的补充功能候选池。

建议后续如需正式实施，作为 `E2-F5 外部视频与 AI 编排工作流收口` 进入，而不是插回已经收口的 `E2-F2/F3/F4`。

```text
E2-F4 batch/workdir 已形成基础
   ↓
E2-F5 外部视频与 AI 编排工作流收口（候选）
   ↓
再评估是否需要 L2/L3 级别命令实现
```

## 功能候选矩阵

| 编号 | 能力 | 用户价值 | 推荐状态 | Owner 候选 | 不做边界 |
| --- | --- | --- | --- | --- | --- |
| F5-A | 外部视频导入指南 | YouTube / 网页视频 / 会议录屏先安全落本地 | 文档化即可 | docs / workflow skill | 不内置下载器 |
| F5-B | `chapters.json` 工作流约定 | 外部 AI 可输出章节，OVT 可按章节剪辑或建任务 | 先出 schema 草案 | `Core.Editing` 候选 | 不在仓库内做语义分析 |
| F5-C | 章节驱动 batch workdir | 从章节清单生成多个任务目录或 render manifest | 候选 CLI | `Core.Editing` + `Cli` | 不做 UI 交互选择器 |
| F5-D | VTT loader / 字幕片段提取 | 复用下载字幕，不必全靠转写 | 候选 Core 能力 | `Core.Subtitles` | 不复制 Python 正则脚本 |
| F5-E | 双语字幕合成 | 外部翻译后稳定生成 bilingual SRT / ASS | 候选 Core 能力 | `Core.Subtitles` | 不内置翻译 API |
| F5-F | 发布文案工作目录约定 | 将 AI 文案保留在任务目录，方便发布复用 | 文档化即可 | docs / scaffold outputs | OVT 不解析文案语义 |
| F5-G | `yt-dlp` import 命令 | 一条命令下载视频和字幕 | 暂缓，需边界卡 | `Core.Execution` 候选 | 不绕过 process runner |
| F5-H | libass / subtitles filter 能力探测 | 提前发现硬字幕环境问题 | 可并入后续 doctor 扩展 | `Core.Execution` | 不做平台私有安装器 |

## 分层路线

### L0：文档化工作流

状态：当前可做，低风险。

范围：

- README / Features 增加外部视频和外部 AI 工作流说明
- 仓库内 workflow skill 指导代理如何调用现有 CLI
- 明确 `yt-dlp`、章节分析、翻译、文案都在 OVT 外部

验收：

- 用户能按文档完成“外部下载 -> OVT scaffold/render”
- 外部 AI 能按 skill 不直接拼 FFmpeg 命令
- 文档不把外部工具误写成 OVT 内置命令

### L1：结构化文件契约草案

状态：已开始。

范围：

- 草拟 `chapters.json`
- 草拟 `publish-copy.md` / `copy.json` 的工作目录约定
- 明确外部翻译字幕的命名和接回路径

当前落地物：

- `docs/schemas/chapters.schema.json`
- `examples/external-ai-video-workflow/signals/chapters.sample.json`
- `examples/external-ai-video-workflow/tasks/chapter-02/copy.sample.md`
- `examples/external-ai-video-workflow/commands.example.ps1`
- `examples/external-ai-video-workflow/render-batch.sample.json`

`chapters.json` 最小形态：

```json
{
  "schemaVersion": 1,
  "source": {
    "input": "source.mp4",
    "text": "transcript.json"
  },
  "chapters": [
    {
      "id": "chapter-02",
      "title": "企业 vs 消费者：战略选择",
      "start": "00:02:40.000",
      "end": "00:04:40.000",
      "summary": "外部 AI 生成的章节摘要",
      "keywords": ["AI", "enterprise", "consumer"]
    }
  ]
}
```

验收：

- 结构能覆盖 YouTube Clipper 的章节结果
- 结构不要求 OVT 理解自然语言
- 时间格式、路径解析和 ID 规则可测试

### L2：确定性字幕与章节消费能力

状态：需要实施卡。

范围候选：

- `Core.Subtitles` 支持 VTT loader
- `Core.Subtitles` 支持按时间区间提取字幕片段并重置时间戳
- `Core.Subtitles` 支持英文/译文合成双语 SRT
- `Core.Editing` 支持从章节清单投影 clips 或任务 skeleton

可能命令形态：

```text
ovt subtitle-clip --subtitles source.en.vtt --from 00:02:40.000 --to 00:04:40.000 --output chapter-02.srt
ovt merge-subtitles --primary chapter-02.en.srt --secondary chapter-02.zh.srt --output chapter-02.bilingual.srt
ovt scaffold-chapters --input source.mp4 --chapters chapters.json --template shorts-captioned --dir tasks
```

注意：命令名只是候选，不进入实现前不得写入 command reference。

验收：

- 不直接调用 shell 字符串
- 执行类能力复用 `Core.Execution`
- 字幕能力有模型/格式测试
- CLI 输出遵守 command envelope 和 `--json-out`

### L3：外部下载器接入

状态：暂缓，必须单独开边界卡。

范围候选：

- `doctor` 增加 optional `yt-dlp`
- `Core.Execution` 增加下载器 process adapter
- CLI 提供稳定 `import-video` / `download-video` envelope

前置决策：

- 是否承担 YouTube / 平台下载能力的产品边界
- 如何写版权和平台条款提示
- 是否允许 URL 输入进入正式 CLI 契约
- 是否允许 cookies / proxy / auth 等复杂下载配置

必须停下的情况：

- 需要在仓库内维护平台适配策略
- 需要把下载器做成不可审计黑盒
- 需要为下载引入持久化账号、cookies 或跨进程服务

## Owner 边界

| 事项 | Owner | 说明 |
| --- | --- | --- |
| 外部下载步骤说明 | docs / workflow skill | 只作为用户前置动作 |
| `yt-dlp` 真正进程执行 | `Core.Execution` 候选 | 只有 L3 才考虑 |
| VTT / SRT / ASS 解析与输出 | `Core.Subtitles` | 不在 CLI 复制字幕模型 |
| 章节到 clips / plan 投影 | `Core.Editing` | CLI 只装载输入和输出 envelope |
| 渲染、硬字幕、音频混合 | `Core.Execution` | 不在 Desktop/CLI 拼 FFmpeg |
| 外部 AI 章节/翻译/文案 | 仓库外 | 只通过文件协作 |
| 工作目录与 batch artifact | `OpenVideoToolbox.Cli` | 复用现有 batch/workdir 约定 |

## 风险

### 架构风险

- 把下载器、字幕处理、AI 章节分析混成一个“智能剪辑命令”，会直接破坏现有 owner。
- 如果先做 `yt-dlp`，会把平台、网络、版权和配置复杂度提前带进核心。
- 如果直接移植 Python 脚本，会绕开统一 process runner 和 command envelope。

### 重复实现风险

- `cut` 已有，不能再做一套 FFmpeg clipper。
- `subtitle` 已有，VTT 和双语只能扩展 `Core.Subtitles`，不能散落在 CLI。
- `scaffold-template-batch` / `render-batch` 已有，章节批量必须复用现有 batch 语义。

### 产品边界风险

- YouTube 下载不是普通本地媒体处理；需要明确用户有权处理内容。
- 外部 AI 生成结果可能错误；OVT 应提供验证和预览，不承担语义正确性。

## 推荐实施顺序

1. 完成 L0 文档和 workflow skill（已开始）
2. 新增 `chapters.json` schema 草案和样例（已开始）
3. 评估 `subtitle-clip` / `merge-subtitles` 是否值得进入 L2
4. 再评估 `scaffold-chapters` 或章节 batch 工作目录
5. 最后单独判断是否进入 L3 下载器接入

## 阶段验收模板

```text
阶段：
新增能力是否仍通过 CLI + 结构化文件协作：
是否新增外部依赖：
是否引入 AI provider：
是否复用既有 owner：
是否新增或改变 CLI 命令契约：
是否补测试或说明为何只需文档：
是否同步 README / Features / MODULE / external dependencies：
```

## 当前收口判断

本计划本身不改变当前活跃阶段，只提供后续候选路线。

如果现在停止，仓库仍处于一致状态：

- 已有 CLI 能处理本地媒体、字幕、模板、批量和渲染
- 外部视频下载仍是用户侧动作
- 外部 AI 仍只通过文件和 CLI 编排
- 新能力还没有进入正式命令契约
