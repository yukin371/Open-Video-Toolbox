# Desktop 工作台设计

## 1. 页面结构

首版采用单窗口、三栏信息结构：

- 左栏：输入文件、任务列表和状态筛选。
- 中栏：当前任务的媒体信息、预设、字幕/音频选项和 preview 摘要。
- 右栏：执行进度、结构化结果、原始日志和输出文件。

页面不使用复杂时间线。需要修改精确片段时，首版打开或导出 `edit.json`，由现有文本计划承接。

## 2. 任务状态

```text
Draft -> Validating -> Ready -> Queued -> Running
                                      |-> Succeeded
                                      |-> Failed -> RetryQueued
                                      |-> Cancelled
```

UI 只展示和触发状态转换；状态来源是 Core 的执行结果和取消信号。

## 3. 核心交互

### 新建任务

1. 选择文件或目录。
2. 自动 probe，并提示缺失的 ffmpeg/ffprobe/whisper 依赖。
3. 选择预设、输出目录和覆盖策略。
4. 选择字幕/转写/音频输入。
5. 生成计划并显示 preview。
6. 加入队列。

### 执行任务

- 任务行显示当前步骤、百分比、耗时和输出路径。
- 取消只发送给 Core 执行层，不强杀 UI 状态。
- 失败展开 stderr、命令计划和相关输入路径。
- 重试复用原计划，只有用户明确修改时才生成新变体。

## 4. ViewModel 边界

- `WorkbenchViewModel`：窗口导航和当前工作区。
- `TaskQueueViewModel`：队列展示、排序、取消和重试命令。
- `TaskEditorViewModel`：预设、路径、字幕和音频选项。
- `ExecutionLogViewModel`：结构化状态与原始日志展示。

这些 ViewModel 不持有 FFmpeg 参数拼接规则、不解析字幕格式、不实现转写或渲染算法。

## 5. 可观察性

每个任务至少展示：输入路径、计划路径、预设、当前步骤、开始/结束时间、退出状态、stdout、stderr、输出产物和失败原因。日志应支持保存到任务工作目录，保证 GUI 关闭后仍可复查。
