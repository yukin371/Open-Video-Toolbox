# OpenVideoToolbox.Core.Jobs

> 最后更新：2026-10-13

## 职责

`Core.Jobs` 持有一次传统转码任务的输入、预设、输出位置和执行安全选项，供 `Core.Execution` 构建并运行命令。

## 数据流

CLI 或 Desktop 解析输入与输出规则 -> 创建 `JobDefinition` / `JobOutput` -> `Core.Execution` 生成临时输出、执行转码并提交正式产物。

## 约定 & 陷阱

- `JobOutput` 的路径和文件名是计划数据，不负责磁盘写入。
- `OverwriteExisting` 只表达用户是否允许替换既有正式产物。
- `AllowLargerOutput` 默认关闭；它只允许显式接受更大的转码结果，不表示画质改善。
- `VideoWatermarkSettings` 只描述水印图片、九宫格位置、边距和透明度；它不包含 FFmpeg 字符串，命令映射由 `Core.Execution` 承接。
- 源文件路径必须与正式输出路径不同；默认输出命名由 CLI 负责，执行层负责保护和提交。
- 修改输出安全字段、路径语义或任务序列化时，同步 `Core.Execution`、CLI 文档和相关测试。
