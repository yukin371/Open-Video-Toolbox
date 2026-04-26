# E2 窄卡：D1 重判证据收口

最后更新：2026-04-26

## 目的

在 `2026-05-12` 或之后再次执行 `E2-G1 / D1` 重判前，先把“需要什么证据、谁来提供、什么算满足”写清楚。

这张卡不启动 `D1`，也不新增功能。它只把当前已经明确的阻塞和观察口径，收成一份更容易执行和复核的证据卡，避免到重判日还停留在“应该差不多稳定了”的口头判断。

## 为什么它仍属于 E2

- 它不写 Desktop 代码
- 它不改变 `edit.json schema v1`
- 它不扩 `V2`
- 它只服务于当前 `E2-G1` 已确认的下一次重判准备

## 为什么不是 D1

- `D1` 的前提仍然未满足：`schema v1` 低频变更窗口尚未完成
- 当前缺的不是更多 Desktop 能力设计，而是更可审计的重判证据
- 如果现在直接写 `D1` 实施稿，会把“是否该启动”与“启动后怎么做”混在一起

## 为什么不是 V2

- 当前问题不是 `timeline`、`effects` 或新编辑模型能力缺失
- 当前问题是 `v1` 稳定窗口和 Desktop 启动纪律
- 因此它应继续留在 `E2`，而不是转去 `V2-P*`

## 范围

- 固定下一次 `E2-G1` / `D1` 重判所需的最小证据包
- 明确哪些证据可以来自已有自动化护栏，哪些仍需人工判断
- 把 `schema v1` 稳定窗口、CLI 工作流稳定性、Desktop 价值判断拆成可逐项复核的输入
- 给出重判当日应执行的最小步骤，而不是再临时拼检查清单

## 不做

- 不启动 `D1`
- 不新增 Desktop 模块文档
- 不修改 `edit.json schema v1` 字段
- 不新增新的 CLI / Core 能力
- 不把这张卡扩成“Desktop 需求分析大全”

## 输入依据

- `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md`
- `docs/plans/2026-04-24-e2-g1-desktop-start-recheck.md`
- `docs/plans/2026-04-25-e2-current-stop-point.md`
- `docs/plans/2026-04-26-e2-schema-v1-stability-guardrail-phase-check.md`
- `docs/roadmap.md`

## 计划收口的问题

这张卡只回答四个问题：

1. 到 **2026-05-12** 时，如何判定 `schema v1` 观察窗口是否真的成立？
2. 哪些 CLI / workflow 稳定性证据应视为“仍然满足”，而不需要重新大范围审计？
3. “Desktop 是否比继续做 E2 更值得”要靠哪些输入判断？
4. 重判后允许的结果和后续动作分别是什么？

## 建议产出

1. 一份 `E2-G1` 重判证据表
   - 列出每项门槛
   - 列出证据来源
   - 列出判断人可直接执行的检查动作
2. 一份重判日执行顺序
   - 先看自动化护栏是否有 breaking change 迹象
   - 再看 CLI / workflow 是否仍维持当前稳定结论
   - 最后单独判断 Desktop 价值，而不是提前默认要做
3. 一份结果分流表
   - 结果 A：继续留在 `E2`
   - 结果 B：正式启动 `D1`

## 重判证据表

| 证据项 | 当前状态 | 证据来源 | 证据类型 | 重判日动作 | 满足标准 |
| --- | --- | --- | --- | --- | --- |
| `schema v1` 低频变更窗口 | 当前未满足，最早观察完成日收敛为 **2026-05-12** | `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md`、`docs/plans/2026-04-24-e2-g1-desktop-start-recheck.md`、`docs/plans/2026-04-26-e2-schema-v1-stability-guardrail-phase-check.md` | 自动化护栏 + 人工确认 | 检查观察窗口内是否出现新的 `schema v1` breaking change；复核护栏相关测试与文档结论未失效 | 从 `2026-04-22` 起到重判日之间无新的 `schema v1` breaking change，且当前护栏仍成立 |
| CLI / workflow 稳定性是否仍成立 | 当前基本满足 | `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md`、`docs/plans/2026-04-24-e2-f4-phase-check.md`、`docs/roadmap.md` | 既有阶段结论 + 人工抽查 | 复核当前没有新的主线改动推翻既有结论；必要时抽查 `templates -> init-plan / scaffold-template -> validate-plan -> render` 是否仍沿既有路径成立 | 没有新的证据表明 CLI 主工作流已失稳，也没有新的 owner 漂移 |
| Desktop 是否仍被限定为交互壳 | 当前满足 | `docs/ARCHITECTURE_GUARDRAILS.md`、`docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md` | 文档规则 | 确认自上次重判后没有文档或代码把 Desktop 推成新业务 owner | Desktop 仍只被定义为消费稳定 Core/CLI 边界的交互壳 |
| Desktop 是否比继续做 E2 更值得 | 当前未确认 | `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md`、`docs/roadmap.md` | 人工判断 | 单独回答“现在做 Desktop 的收益，是否高于继续做 CLI / 生态收敛” | 有明确、可陈述的交互壳收益，而不是因为“差不多可以做了” |
| D1 首轮范围是否仍足够小 | 当前有建议，但未正式承诺 | `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md` | 人工判断 | 复核首轮闭环仍压缩为“导入素材 → 选模板 → 编辑少量参数 → 执行 → 看结果与日志” | 没有把时间线、批处理、设置持久化等 `D2` 范围偷带进来 |

## 自动化证据与人工判断边界

### 自动化证据

- `schema v1` Core snapshot 护栏仍成立
- canonical `schemaVersion = 1` 样本仍可反序列化并通过 validator
- 当前 solution 仍可成功 build

这些证据用于说明：当前仓库没有显式出现新的 `schema v1` 契约漂移信号。

### 人工判断

- Desktop 是否比继续留在 `E2` 更有收益
- Desktop 首轮范围是否仍压缩到最小闭环
- 当前 CLI / workflow 的稳定性是否需要重做大范围人工复核，还是只需抽查

这些问题不能被测试自动回答，必须在重判时单独写出判断理由。

## 重判日执行顺序

建议在 **2026-05-12** 或之后按以下顺序执行：

## 待补齐表格

| 条目 | 当前是否已齐 | 待补内容 | 来源文档 / 产物 | 负责人 | 目标日期 | 备注 |
| --- | --- | --- | --- | --- | --- | --- |
| `schema v1` 观察窗口结论 | 否 | 填写从 `2026-04-22` 到重判日前是否出现新的 breaking change | Core snapshot 护栏、相关提交记录、`docs/plans/2026-04-26-e2-schema-v1-stability-guardrail-phase-check.md` | 待定 | 2026-05-12 | 这是唯一硬门槛 |
| CLI / workflow 稳定性抽查结论 | 否 | 记录是否仍需重做人工抽查，以及抽查结果 | `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md`、`docs/plans/2026-04-24-e2-f4-phase-check.md` | 待定 | 2026-05-12 | 没有新主线改动时应尽量复用既有结论 |
| Desktop 交互壳边界复核 | 否 | 确认没有新文档或新实现让 Desktop 成为业务 owner | `docs/ARCHITECTURE_GUARDRAILS.md`、`docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md` | 待定 | 2026-05-12 | 主要是规则复核，不是功能设计 |
| Desktop 价值判断 | 否 | 写明“为什么现在做 Desktop 比继续做 E2 更值得” | `docs/roadmap.md`、重判当日判断说明 | 待定 | 2026-05-12 | 需要明确理由，不能写成“差不多可以做了” |
| D1 首轮最小范围确认 | 否 | 复核第一轮是否仍只承诺最小闭环 | `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md` | 待定 | 2026-05-12 | 防止把 D2 范围提前混入 |

1. 先看自动化护栏
   - `schema v1` 稳定窗口相关测试是否仍通过
   - 观察窗口内是否已出现新的 breaking change 记录
2. 再看既有阶段结论是否仍有效
   - `E2-F4` 与当前 CLI 主工作流结论是否被新的改动推翻
   - Desktop 交互壳边界是否仍未漂移
3. 最后单独判断是否值得启动 `D1`
   - 不是问“能不能做”
   - 而是问“现在做是否比继续留在 E2 更值”

## 重判结果分流

### 结果 A：继续留在 E2

适用条件：

- `schema v1` 观察窗口仍未满足
- 或 Desktop 价值判断仍不够明确
- 或 D1 首轮范围已开始膨胀

后续动作：

1. 保持 `D1` 仍未正式启动
2. 在 `docs/roadmap.md` 记录新的重判结论和下一次观察点
3. 如仍需继续推进，只允许重开新的明确 `E2` 卡

### 结果 B：正式启动 D1

适用条件：

- `schema v1` 观察窗口满足
- Desktop 价值判断明确成立
- Desktop 交互壳边界仍保持单一
- 首轮闭环仍压缩到最小可交付范围

后续动作：

1. 在 `docs/roadmap.md` 把当前状态改为正式启动 `D1`
2. 新建 `src/OpenVideoToolbox.Desktop/MODULE.md`
3. 新建 `D1` 正式实施计划
4. 明确第一轮只做无时间线的最小闭环

## 重判日输出模板

```text
阶段：E2-G1
执行日期：
schema v1 观察窗口是否满足：
CLI / workflow 稳定性是否仍成立：
Desktop 是否仍被限定为交互壳：
Desktop 是否比继续做 E2 更值得：
D1 首轮范围是否仍压缩到最小闭环：
最终结果：结果 A / 结果 B
后续动作：
```

## 完成标准

只有同时满足以下条件，才算这张卡完成：

1. 下一次 `E2-G1` 重判需要的证据项已被逐条枚举
2. 每条证据都已有明确来源，而不是靠临时回忆
3. 自动化证据与人工判断的边界已分开
4. 重判结果和后续动作已被写成可执行分流，而不是开放式讨论
5. `docs/roadmap.md` 已把这张卡标成继续推进时的首选新卡

## stop-point

这张卡的合理 stop-point 不是“启动 D1”，而是：

- 重判证据包已经写清楚
- 后续只需在到期日按卡执行
- 在此之前不再继续横向扩张同类准备文档
