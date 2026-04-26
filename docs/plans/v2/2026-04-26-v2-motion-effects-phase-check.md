# V2 Motion Effects 阶段检查：首期最小闭环

最后更新：2026-04-26

## 目的

这份文档只回答一个问题：

> 简易 `motion effects` 的第一期实现，是否已经在当前仓库里形成了可回归、可验收、边界仍然收敛的最小闭环？

这里检查的不是“效果够不够炫”，而是确认：

- `move / orbit / zoompan` 已打通 `discover -> validate -> render --preview / execute`
- effect 参数 schema、validator 与 execution path 仍各守 owner
- 当前实现没有把仓库拉向 `Live2D`、角色动画器或第二套关键帧系统
- 当前阶段已经具备手动验收包，可以进入人工判断

## 当前纳入范围

本轮只纳入：

- built-in effect descriptors：
  - `move`
  - `orbit`
  - `zoompan`
- `EditPlanValidator` 的 built-in parameter validation
- `FfmpegTimelineRenderCommandBuilder` 的 executor path 接线
- `effects` / `validate-plan` / `inspect-plan` / post-mutation validation 的一致性
- motion effect 对应 Core / CLI tests
- motion effect 的文档同步与阶段记录

本轮明确不纳入：

- 插件 effect 加载
- track 级 motion effect
- placeholder visual motion
- 角色部件绑定、面部驱动、口型、表情参数
- 通用关键帧编辑器、曲线编辑器、动画 graph
- Desktop 动效编辑 UI

## 当前已落地能力

### Core.Editing

当前已落地：

- `BuiltInEffectCatalog` 已新增：
  - `move`
  - `orbit`
  - `zoompan`
- 三者统一归类到 `animation`
- `EditPlanValidator` 已新增 built-in effect 参数级校验：
  - required
  - type
  - enum
  - range
- motion usage 限制已被显式锁住：
  - 仅支持 clip 级 effect
  - 仅支持 `video` clip
  - 不支持 placeholder clip
  - 必须显式提供 `clip.src`

### Core.Execution

当前已落地：

- `FfmpegTimelineRenderCommandBuilder` 已支持 template path 与 executor path 并存
- motion executor 已落地：
  - `MoveEffectExecutor`
  - `OrbitEffectExecutor`
  - `ZoomPanEffectExecutor`
- `move` / `orbit` / `zoompan` 已可被 `render --preview` 和真实执行路径共同消费

当前已守住的边界：

- motion 语义没有回流到 CLI
- `edit.json` 没有暴露 raw `ffmpeg` expression 输入面
- 当前仍是“单对象、确定性、受限 executor path”，不是角色动画系统

### Cli

当前已落地：

- `effects list/describe` 可稳定发现首期 motion effects
- `inspect-plan` 现会带上同一份 built-in effect registry 做一致性校验
- `replace-plan-material`
- `attach-plan-material`
- `bind-voice-track`
- 上述 mutation follow-up validation 已与 `validate-plan` 统一 effect registry，不再漂移

## 与阶段目标对照

### 条件 1

> motion effects 必须保持为当前 `v2 timeline + effects + render` 路径内的窄增量，而不是新建第二套 owner

当前判断：**满足**

### 条件 2

> `effects` discovery、`validate-plan` 和执行路径必须共用同一套 built-in effect 定义，而不是各有一套语义

当前判断：**满足**

### 条件 3

> 首期只能支持 `move / orbit / zoompan`，且必须显式限制作用对象

当前判断：**满足**

说明：

- 当前仅支持 clip 级
- 当前仅支持 `video`
- 当前要求显式 `clip.src`
- 当前拒绝 placeholder

### 条件 4

> preview 和 execute 必须复用同一路径，不能只在文档或测试里“宣称支持”

当前判断：**满足**

### 条件 5

> 本轮不能把 scope 漂移到 `Live2D`、角色 rig、面部驱动或关键帧工作台

当前判断：**满足**

说明：

- 本轮只新增 deterministic 2D motion primitive
- 没有新增动画工程格式
- 没有新增 UI / graph / track editor

### 条件 6

> 必须有足够回归保护，避免 effect schema、validator 和 filter graph 再次分裂

当前判断：**满足**

## 当前验证结果

本轮已执行：

```powershell
dotnet test .\src\OpenVideoToolbox.Core.Tests\OpenVideoToolbox.Core.Tests.csproj --filter "FullyQualifiedName~BuiltInEffectCatalogTests|FullyQualifiedName~FfmpegTimelineRenderCommandBuilderTests|FullyQualifiedName~V2TimelineSkeletonTests"
dotnet test .\src\OpenVideoToolbox.Cli.Tests\OpenVideoToolbox.Cli.Tests.csproj
dotnet build OpenVideoToolbox.sln
dotnet test OpenVideoToolbox.sln
```

当前结果：

- `OpenVideoToolbox.Core.Tests`：186 通过
- `OpenVideoToolbox.Cli.Tests`：195 通过
- 总计：381 通过
- `dotnet build OpenVideoToolbox.sln`：通过

## 手动验收入口

如需做人工阶段验收，最小入口建议为：

1. `effects list`
   - 预期：能看到 `move`、`orbit`、`zoompan`
2. `effects describe move`
   - 预期：`templateMode = "executor"`，且参数 schema 与 spec 一致
3. `render --plan <motion-edit.json> --preview`
   - 预期：返回非空 `executionPreview`
   - 预期：filter graph 内包含 motion executor 生成片段，而不是空 effect
4. `validate-plan --plan <motion-edit.json>`
   - 预期：合法 plan 通过；非法 easing / 缺参 / placeholder 场景能报结构化 issue

通过标准：

- 三个 built-in motion effect 都可被发现
- validator 与 preview 结果一致
- 没有出现“CLI 说支持，但 render 路径实际跳过”的断层
- 没有新增超出首期边界的效果或入口

不通过标准：

- effect descriptor、validator 和 execution path 任一处语义不一致
- preview 缺少 motion filter graph
- `clip.src` / placeholder 限制失效或被隐式绕过
- 范围开始滑向角色动画或第二套动画平台

## 当前结论

`V2 motion effects` 当前判断为：**首期 implementation 与 phase-check 已完成，当前状态可视为 `ready_for_acceptance`**

更准确地说：

- `move / orbit / zoompan` 已不是纯文档设计
- 当前仓库已具备受限、可回归、可脚本消费的最小 motion effects 闭环
- 当前仍守住了“不做 Live2D / 不做角色动画器 / 不做关键帧工作台”的边界

因此下一步不应继续无边界追加 effect 类型或动画主题；更合理的动作是进入人工阶段验收，并在验收后决定：

1. 接受当前 stop-point
2. 留在该主题补最小缺口
3. 回退 scope
4. 暂停整条 motion 子线
