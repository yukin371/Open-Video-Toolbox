# E2 窄卡阶段检查：schema v1 稳定窗口自动化护栏

最后更新：2026-04-26

## 目的

这份阶段检查只回答一个问题：

> 当前这张 `schema v1` 稳定窗口自动化护栏窄卡，是否已经达到合理的 stop-point？

它不新增功能范围，只把当前已经落地的 snapshot、语义校验和目录维护规则，与窄卡目标逐条对照，避免继续为了“还能再补一点”而把同一张卡无限拉长。

## 当前结论

当前判断为：**已完成当前范围，可回到 E2 stop-point。**

更准确地说：

- Core 已锁定 canonical `schemaVersion = 1` 的完整样本与最小样本
- 任意序列化字段或 shape 漂移会在 snapshot 测试中显式失败
- snapshot 不再只是文本护栏；当前还要求样本可反序列化，并通过 `EditPlanValidator`
- full 样本对 template context / artifact slot 的真实依赖已经显式编码进测试，不再靠隐式背景知识
- `OpenVideoToolbox.Core.Tests/snapshots/README.md` 已补目录级维护规则，避免后续直接改 golden file 掩盖回归

## 为什么现在适合停

当前这张卡的目标已经是“把稳定窗口推进成更直接的自动化护栏”，而不是“把所有 v1 plan 变体都做成 snapshot 矩阵”。

继续往下补更多样本，会开始出现以下问题：

1. snapshot 覆盖开始膨胀
2. 审阅成本会上升，但对 `E2-G1` 的新增价值下降
3. 容易把本应是新明确卡片的工作，继续塞回同一张护栏卡里

因此当前更合理的动作不是继续横向加样本，而是：

- 保留这层护栏
- 等待新的明确 E2 卡
- 或在时间窗口满足后重做 `E2-G1 / D1` 判断

## 本轮产出

- `src/OpenVideoToolbox.Core.Tests/EditPlanSerializationTests.cs`
- `src/OpenVideoToolbox.Core.Tests/snapshots/edit-plan-schema-v1.json`
- `src/OpenVideoToolbox.Core.Tests/snapshots/edit-plan-schema-v1-minimal.json`
- `src/OpenVideoToolbox.Core.Tests/snapshots/README.md`
- `docs/plans/2026-04-26-e2-schema-v1-stability-guardrail.md`

## 验证

- `dotnet test .\\src\\OpenVideoToolbox.Core.Tests\\OpenVideoToolbox.Core.Tests.csproj --filter FullyQualifiedName~EditPlanSerializationTests`
- `dotnet build .\\OpenVideoToolbox.sln`

## 阶段检查输出

```text
阶段：E2 schema v1 stability guardrail
阶段目标是否完成：是
当前 owner 是否保持单一：是，字段语义仍归 Core.Editing，护栏归 Core.Tests
是否出现第二套模型或第二套语义：否
当前自动化护栏是否已形成稳定闭环：是
下一步是否仍应继续在本卡内扩张：否
如果现在停止，仓库是否仍处于一致状态：是
```
