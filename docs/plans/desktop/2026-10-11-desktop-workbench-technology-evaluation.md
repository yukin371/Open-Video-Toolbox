# Desktop 工作台技术评估

## 1. 评估问题

需要选择一个能承载本地文件、任务队列、日志和表单交互的桌面框架，同时保持 `.NET 8 + C#`、Core 复用和跨平台发布能力。

## 2. 候选比较

| 方案 | 优点 | 主要问题 | 判断 |
| --- | --- | --- | --- |
| Avalonia | 跨平台、C# 复用、适合列表/表单/日志、与现有技术路线一致 | 需要引入 UI 依赖和学习成本 | 首选 |
| WPF | Windows 体验成熟，资料多 | Windows-only，和跨平台目标冲突 | 备选 |
| WinUI 3 | Windows 原生体验 | 发布、运行时和跨平台能力受限 | 不作为首选 |
| Electron | Web UI 迭代快，生态丰富 | 体积和资源占用较高，需要 Node 运行时 | 不采用 |
| Tauri | 体积小，跨平台 | 引入 Rust/Web 双栈，当前团队成本高 | 暂不采用 |

## 3. 推荐结论

继续采用 Avalonia，原因是：

- `docs/tech-stack.md` 已将其列为首选。
- Desktop 可以直接复用 Core 的 C# 模型和异步执行能力。
- 任务队列、日志面板、预设编辑和文件选择都属于 Avalonia 的常规场景。
- 后续可保留 Windows 优先，同时为 Linux/macOS single-file 发布留出空间。

## 4. 依赖与风险

- 正式引入前需要确认 Avalonia 版本、控件库许可和发布体积。
- UI 层不得直接引用 CLI；需要的呈现模型应下沉到 Core 或独立 application facade。
- 队列状态、取消和重试不能由 ViewModel 私自模拟，必须连接 `Core.Execution`。
- 长任务必须支持取消、超时和原始 stdout/stderr 采集。
- UI 自动化只覆盖关键交互，核心规则继续由 Core/CLI 测试覆盖。

## 5. 外部工具策略

- `ffmpeg` / `ffprobe`：继续由 `Core.Execution` 统一调用。
- `whisper.cpp`：作为可选本地依赖，优先用于音频转文字，降低外部 AI 成本。
- 翻译：通过外部生成的 SRT/VTT/结构化字幕文件接入，不引入 provider SDK。
- `yt-dlp`：仍是用户侧外部导入步骤，D1 不纳入下载器。
