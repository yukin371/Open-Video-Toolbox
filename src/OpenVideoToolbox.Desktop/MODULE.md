# OpenVideoToolbox.Desktop

> 最后更新：2026-04-24

## 职责

Desktop 是未来的交互壳。它的职责是把已经由 `Core` 与 CLI 验证过的能力，以更适合人工操作的方式组织成图形界面。

它不是新的业务 owner，也不是第二套执行内核。

## Owns

- Desktop 入口与应用启动组织
- 未来的 UI framework glue
- 页面导航、视图状态与交互事件组织
- 对以下稳定对象的展示和编辑：
  - 模板 / 插件发现结果
  - `edit.json`
  - `validate-plan` 结果
  - `render` / `mix-audio` preview 结果
  - 执行日志、produced paths、错误摘要
- 小丸工具箱式本地工作台的任务队列、预设转码、字幕/音频处理入口和输出管理
- 文件选择、结果查看、最小表单式参数编辑

## Must Not Own

- `ffmpeg` / `ffprobe` 命令拼接
- 外部进程启动、超时、取消和输出采集
- 模板 schema、插件 schema、`edit.json` schema
- 音频分析、节拍分析、字幕生成、转写解析规则
- 第二套独立于 `edit.json` 的编辑模型
- 任何内置 AI provider / SDK / 远程推理逻辑

## 关键依赖

- `OpenVideoToolbox.Core`

说明：

- Desktop 可以依赖 `Core`
- Desktop 不得直接依赖 `Cli`
- 如需复用 CLI 级别的呈现语义，应下沉为 `Core` 可复用模型，而不是让 Desktop 反向调用 CLI 入口

## 不变量

- Desktop 只是交互壳，不是新的业务 owner
- Desktop 只能消费与展示稳定边界对象，不重新解释核心规则
- Desktop 不得旁路 `Core.Execution` 直接启动外部工具
- Desktop 必须把 `edit.json` 视为唯一计划边界，不得发明 UI 私有计划格式
- Desktop MVP 第一轮只允许覆盖“导入素材 -> 选模板 -> 编辑少量参数 -> 校验 -> 预览 -> 执行 -> 看日志”这一条闭环
- 在上述闭环中允许加入批量任务队列、进度、取消和重试，但这些状态必须来自 `Core.Execution`

## 当前状态

- 当前目录已引入 Avalonia 11.3.12，形成首个工作台壳
- 当前已建立文件选择、队列展示、预设/输出表单、字幕/音频选择和日志面板
- 当前界面已收敛为顶部全局工具栏、左侧队列、中间分 Tab 工作区、右侧任务详情和底部日志；按钮样式明确区分主操作、普通操作和危险操作
- 当前已接入真实 `Core.Execution` 和本地任务队列持久化；队列支持按顺序批量执行、失败后继续、取消后保留未执行任务，并持久化每项状态、日志、错误和产物路径。字幕工作流已支持 SRT/VTT 选择、外部翻译字幕留口、双语字幕确定性合并、本地 `WhisperCppTranscriptionService` 转写，以及由 Core 执行链承接的 sidecar/burn-in
- 当前主场景：小丸工具箱式本地批量转码与字幕/音频处理工作台

## 启动前必须明确的事项

1. UI framework owner
2. 状态管理 owner
3. Desktop 首轮只消费哪些稳定对象
4. 哪些能力必须继续停留在 `Core`

## 文档同步触发条件

- Desktop 引入实际 UI framework
- Desktop 新增页面 / 状态管理 / 日志查看等长期结构
- Desktop 改变允许依赖或 owner 边界
