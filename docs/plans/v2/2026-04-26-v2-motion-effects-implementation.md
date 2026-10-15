# V2 Motion Effects 首期最小实现

最后更新：2026-04-26

## 卡片信息

```text
卡片编号：V2-M1
所属阶段：V2 motion effects
卡片类型：执行
目标：在不扩张产品边界的前提下，落地首期 motion effects 最小闭环
输入：docs/plans/v2/2026-04-26-v2-motion-effects-spec.md、docs/plans/v2/2026-04-26-v2-motion-effects-plan.md
输出：Core.Editing / Core.Execution / Cli glue / tests / 文档同步
完成标准：move / orbit / zoompan 已打通 discover -> validate -> render preview / execute 闭环
下一张卡：如继续，只允许开 motion effects phase-check
```

## 本轮落地

1. `Core.Editing`
   - `BuiltInEffectCatalog` 新增：
     - `move`
     - `orbit`
     - `zoompan`
   - 三者统一归类到 `animation`
   - 参数 schema 固定收口到 built-in descriptor
   - `EditPlanValidator` 新增 built-in effect 参数级校验：
     - required
     - type
     - enum
     - range
   - 首期 motion usage 限制已显式锁住：
     - 仅支持 clip 级 effect
     - 仅支持 video clip
     - 不支持 placeholder clip
     - 必须显式提供 `clip.src`

2. `Core.Execution`
   - `FfmpegTimelineRenderCommandBuilder` 已建立 template path 与 executor path 并存的 effect 执行链
   - 新增 motion executor：
     - `MoveEffectExecutor`
     - `OrbitEffectExecutor`
     - `ZoomPanEffectExecutor`
   - motion 表达式统一走 executor 生成，不把原始 `ffmpeg` 表达式暴露进 `edit.json`
   - `move` / `orbit` 当前通过透明背景 + `overlay` 路径执行
   - `zoompan` 通过专用 motion helper 映射到稳定 filter graph

3. `Cli`
   - `effects list/describe` 现可稳定发现首期 motion effects
   - `inspect-plan`
   - `replace-plan-material`
   - `attach-plan-material`
   - `bind-voice-track`
   - 上述 follow-up validation 现与显式 `validate-plan` 共用同一份 built-in effect registry，避免 mutation 路径与 validation 路径漂移

4. 测试
   - `BuiltInEffectCatalogTests`
     - 新增 `move / orbit / zoompan` discovery 与 schema 回归
   - `V2TimelineSkeletonTests`
     - 新增 motion effect validator 回归
   - `FfmpegTimelineRenderCommandBuilderTests`
     - 新增 motion filter graph 生成回归
   - `CommandArtifactsIntegrationTests`
     - 新增 `effects` 命令 contract 回归
     - 新增 `render --preview` motion timeline contract 回归

## 当前 contract

首期只正式支持：

1. `move`
2. `orbit`
3. `zoompan`

首期使用限制：

1. 只支持 `schemaVersion = 2`
2. 只支持 timeline `video` clip
3. 只支持 clip 级 effect
4. clip 必须显式提供 `src`
5. 不支持 placeholder clip

这意味着当前实现目标是：

- 简易 deterministic 2D motion
- 可发现
- 可校验
- 可 preview
- 可执行

而不是：

- `Live2D`
- 角色绑定 / rig
- 面部驱动
- 自由关键帧系统
- 多对象联动动画器

## 明确不纳入

1. 插件 effect 加载
2. track 级 motion effect
3. placeholder visual motion
4. 第二套 raw `ffmpeg` expression 输入模型
5. Desktop 时间线动效编辑 UI
6. 角色动画器 / 骨骼系统 / 表情驱动

## 验证

```powershell
dotnet test .\src\OpenVideoToolbox.Core.Tests\OpenVideoToolbox.Core.Tests.csproj --filter "FullyQualifiedName~BuiltInEffectCatalogTests|FullyQualifiedName~FfmpegTimelineRenderCommandBuilderTests|FullyQualifiedName~V2TimelineSkeletonTests"
dotnet test .\src\OpenVideoToolbox.Cli.Tests\OpenVideoToolbox.Cli.Tests.csproj
dotnet build OpenVideoToolbox.sln
dotnet test OpenVideoToolbox.sln
```

结果：

- `OpenVideoToolbox.Core.Tests` 通过 `186`
- `OpenVideoToolbox.Cli.Tests` 通过 `195`
- solution `build` 通过
- solution `test` 通过
