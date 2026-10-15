# 文档治理指南

本文定义仓库文档的分类、命名和维护规则。`docs/README.md` 是导航入口；本文是分类与命名规则的唯一来源。稳定入口保留在 `docs/` 顶层，专题图、参考材料和版本计划归入分类目录。

## 文档分类

| 类型 | 内容 | 位置与 canonical owner |
| --- | --- | --- |
| 产品与需求 | 产品定位、目标用户、承诺范围 | `docs/PRD.md`；当前功能状态另见 `README.md` 和 `docs/FEATURES_AND_USAGE.md` |
| 问题与分析 | 未解决问题、技术调查、可行性分析 | 现阶段问题记录放在 `docs/analysis/issue/current/`；结案记录可归入相邻的 `archive/`，研究报告放在 `docs/analysis/` |
| 计划与任务卡 | 目标、范围、顺序、owner、依赖、验收条件 | 当前版本计划放在 `docs/plans/v1.0/`，Desktop 与 v2 专项分别放在 `docs/plans/desktop/`、`docs/plans/v2/`；既有历史计划保持原路径并由计划索引导航 |
| 决策 | 已接受且会影响后续实现的架构/产品决策 | `docs/decisions/ADR-NNNN-short-topic.md` |
| 架构与实现 | 系统级结构放架构文档；图源放 diagrams；模块职责、不变量放模块文档 | `docs/architecture.md`、`docs/diagrams/`、`docs/ARCHITECTURE_GUARDRAILS.md`、各模块 `MODULE.md` |
| 验证与发布证据 | 可复现验证步骤、测试报告、release smoke 结果 | 新报告放在 `docs/verification/<version>/`；专项卡中的验收结果仍留在该卡 |
| 用户指南 | 安装、命令、工作流和排障 | `docs/QUICK_START.md`、`docs/FEATURES_AND_USAGE.md`、`docs/COMMAND_REFERENCE.md` 等 |
| 参考资料 | 通用工作手册、已冻结的设计草案等非日常入口 | `docs/reference/` |

### 文档职责

- `README.md`：项目入口、已实现功能矩阵和最短阅读路线。
- `docs/README.md`：仓库全部文档的导航索引，不复制文档正文。
- `docs/roadmap.md`：当前活跃目标、阶段状态和下一步，只链接详细卡片。
- `docs/PRD.md`：相对稳定的用户、产品边界和需求。
- `docs/architecture.md`：当前模块、依赖方向、关键数据流和架构图入口。
- `docs/diagrams/`：架构与流程图源文件；图示入口从 `docs/architecture.md` 进入。
- `docs/ARCHITECTURE_GUARDRAILS.md`：owner、不变量、禁止事项和架构门槛。
- `docs/reference/`：通用仓库初始化手册与历史设计参考，不作为当前 CLI 行为规范。
- `MODULE.md`：所属模块的局部 owner、职责、依赖和测试入口。
- `docs/plans/`：尚未完成或已完成但仍有复用价值的计划、专项卡与阶段检查。
- `docs/decisions/`：已作出的长期决策；尚未决策的问题留在问题记录或计划卡，不提前写成 ADR。

## 命名规则

1. 长期稳定的入口文档使用固定名称，如 `README.md`、`PRD.md`、`roadmap.md`、`architecture.md`，不加日期。
2. 有明确发生时间的分析、计划、阶段检查和验证报告使用完整 ISO 日期：`YYYY-MM-DD-topic.md`。日期表示创建日期，正文中的“最后更新”表示最近修改日期。
3. 使用完整四位年份和两位月份/日期，不使用 `YYMMDD`，避免跨世纪歧义并便于按名称排序。
4. ADR 使用连续编号 `ADR-0001-short-topic.md`。已接受的 ADR 不改编号；被替代时在正文标记替代关系。
5. 版本已由目录表达时，文件名不重复附加版本号，例如 `docs/plans/v2/2026-10-13-render-boundary.md`。
6. 文件名使用小写 kebab-case 和有辨识度的主题；不使用 `final`、`new`、`最新`、`v2-final` 等临时后缀。不同类型由目录表达，不在标题里堆叠“计划/设计/总结/报告”。
7. 仅当目录里存在多个需要独立导航的文档时才增加 `README.md`。避免为每个文件夹机械添加空壳导航。

## 状态与维护

开发者修改前仍遵循 `AGENTS.md` 的必读顺序，再读目标问题/计划卡、相关 ADR、实现与测试。排障可从问题证据进入，功能实现可从需求进入；分类用于存放资料，不要求每项任务都生成一整套阶段文档。

- 任务卡正文开头标注 `状态`、`owner`、`目标` 和 `验收条件`；执行后补验证结果和关联提交/报告。
- 建议状态为 `proposed`、`ready`、`in progress`、`blocked`、`complete`、`superseded`。`roadmap.md` 只复述当前活跃状态，并链接对应卡片。
- 一项内容只保留一个 canonical 文档。其他文档通过链接引用，不复制长期维护的正文或清单。
- Git 历史负责保存文档变更过程，不在文件名里添加版本副本或 `-old` 备份。
- 已有文档按需迁移：更新链接后再移动；每批移动后检查引用。稳定入口和带有大量历史引用的计划卡保留原路径，专题材料归入对应目录。
- 收尾记录本次完成内容、验证结果、未完成项、下一步与关联路径。只有执行过验证并记录结果后才标记 `complete`，避免把计划当作已交付。

## 当前文档布局

- `docs/` 顶层保留稳定入口：项目画像、PRD、Roadmap、架构护栏、快速开始、功能指南和命令速查。
- `docs/diagrams/` 收纳架构图与流程图源文件。
- `docs/reference/` 收纳通用仓库工作手册和历史 CLI 设计参考；日常行为以当前指南和实现为准。
- `docs/plans/v1.0/`、`docs/plans/desktop/`、`docs/plans/v2/` 分别收纳按版本或工作线维护的材料。
- `docs/plans/` 根目录保留早期阶段记录，避免大量历史引用失效；它们只记录当时结论，不能覆盖 Roadmap 的当前状态。

后续编辑根目录的历史计划时，应同步修正引用，并在有明确维护价值时迁入对应主题目录；不为追求目录整齐而批量搬迁历史证据。
