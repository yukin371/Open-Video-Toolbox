# E2 窄卡：schema v1 稳定窗口自动化护栏

最后更新：2026-04-26

## 目的

把 `E2-G1` 当前最大的显式 blocker 再往前推进一步：

> `edit.json schema v1` 仍处在稳定性观察窗口内，不能只靠文档提醒“不要再随便漂移”。

这张卡不新增用户能力，只补一层更直接的自动化护栏，让后续 `schemaVersion = 1` 的字段漂移更容易被测试发现。

## 为什么它仍属于 E2

- 它不启动 `D1`
- 它不扩 `V2`
- 它不新增 CLI / Core 业务语义
- 它只服务于当前 `E2-G1` 已写明的稳定窗口要求

## 范围

- 为 canonical 的 `schemaVersion = 1` 完整 / 最小 plan 样本补稳定序列化黄金文件
- 在 `OpenVideoToolbox.Core.Tests` 增加对应快照回归测试
- 为 `OpenVideoToolbox.Core.Tests/snapshots/` 补目录级维护规则
- 保持现有 round-trip 测试，避免只测文本不测模型回读

## 不做

- 不修改 `schema v1` 字段语义
- 不新增 `edit.json` 新字段
- 不把所有 plan 变体都做成快照矩阵
- 不引入第二套 schema 兼容层

## owner

- `schema v1` 字段语义 owner：`OpenVideoToolbox.Core/Editing`
- 稳定性回归保护 owner：`OpenVideoToolbox.Core.Tests`

## 完成标准

只有同时满足以下条件，才算这张卡完成：

1. canonical `schemaVersion = 1` 完整 / 最小 plan 已有稳定黄金样本
2. 任意字段名或序列化 shape 漂移会在 Core 测试中显式失败
3. 现有 round-trip 校验仍保留，保证不是只有文本快照而没有模型回读
4. Core snapshot 目录已有就地维护规则，避免后续用“直接改 golden file”掩盖回归
5. `docs/roadmap.md` 已把这张窄卡标成当前主线动作，而不是继续回到“随机补东西”

## 验证

- `dotnet test .\\src\\OpenVideoToolbox.Core.Tests\\OpenVideoToolbox.Core.Tests.csproj --filter FullyQualifiedName~EditPlanSerializationTests`
- `dotnet build .\\OpenVideoToolbox.sln`
