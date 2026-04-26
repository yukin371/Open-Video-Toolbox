# V2 Motion Effects 实施计划稿

最后更新：2026-04-26

## 卡片信息

```text
卡片编号：V2-M1
所属阶段：V2 motion effects
卡片类型：计划
目标：为第一期 motion effects 的最小实现定义具体改动面、测试面与文档同步范围
输入：docs/plans/v2/2026-04-26-v2-motion-effects-spec.md、当前 built-in effect catalog / timeline validator / timeline render baseline / effects CLI 测试
输出：本实施计划稿
完成标准：明确 owner、实现顺序、文件落点、测试清单、非目标与回滚路径
下一张卡：如按本计划推进，则进入 motion effects implementation
```

## 实施目标

在不扩张 `E2` 主线、也不把仓库拉向角色动画系统的前提下，补上第一期 motion effects 的最小闭环：

- `move`
- `orbit`
- `zoompan`

并让它们同时满足：

- `effects list/describe` 可发现
- `validate-plan` 可识别并校验
- `render --preview` 可产出稳定 filter graph
- `render` 可复用同一路径执行

## 实施顺序

## 1. Core.Editing built-in effect descriptors

文件：

- `src/OpenVideoToolbox.Core/Editing/IEffectDefinition.cs`

改动：

- 在 `BuiltInEffectCatalog.BuildDefinitions()` 中新增：
  - `move`
  - `orbit`
  - `zoompan`
- 为三个 effect 补齐：
  - `type`
  - `category = animation`
  - 参数 schema
  - 默认值
  - `displayName` / `description`
- `FfmpegTemplates` 第一版建议设为 `null`
  - 明确走 `executor` 路径

目标：

- 先把对外 contract 冻在 `Core.Editing`
- 不让 `Cli` 或测试自己发明第二套参数说明

说明：

- `fade_in` / `fade_out` / `crossfade` 不在这张 implementation 卡里作为主目标推进
- 如后续确需补，也应单独判断是否只是样例配套

## 2. Core.Editing validator 扩展

文件：

- `src/OpenVideoToolbox.Core/Editing/EditPlanValidator.cs`

改动：

- 扩展 `ValidateTimelineEffects(...)`
- 从“只校验 type 是否存在、`auto_ducking.reference` 是否存在”
  提升到“对 built-in effect 做最小参数级校验”

第一期至少补：

1. 通用 built-in effect 参数缺失校验
2. 枚举值校验
   - `easing`
3. 数值区间校验
   - `durationSeconds`
   - `fromScale`
   - `toScale`
   - `radiusX`
   - `radiusY`
4. motion effects 使用场景校验
   - 第一版不支持的对象类型上给出 warning 或 error

建议方式：

- 优先复用 `IEffectDefinition.Parameters`
- 新增一个内部 helper，负责把 `JsonElement` 参数与 schema 对齐
- 不要在 validator 里手写三套完全分裂的参数解析逻辑

目标：

- `validate-plan` 先成为可靠闸门
- 防止 plan 靠“运行时失败”才暴露参数问题

## 3. Core.Execution executor 路径真正接线

文件：

- `src/OpenVideoToolbox.Core/Execution/FfmpegTimelineRenderCommandBuilder.cs`
- `src/OpenVideoToolbox.Core/Execution/IEffectExecutor.cs`
- 新增 `src/OpenVideoToolbox.Core/Execution/*Motion*Executor*.cs`
  - 具体命名可在实现卡决定，但 owner 必须留在 `Core.Execution`

改动：

- 在 render builder 内建立 effect executor registry
- 当 `definition.FfmpegTemplates is null` 时，不再简单跳过
- 对 `move` / `orbit` / `zoompan` 调用专用 executor 生成 filter chain

建议拆分：

1. `MoveEffectExecutor`
2. `OrbitEffectExecutor`
3. `ZoomPanEffectExecutor`
4. 如有必要，再加共享 helper：
   - easing expression helper
   - position expression helper

目标：

- 不把 motion effect 语义塞进 `Cli`
- 不让 `edit.json` 直接暴露原始 `ffmpeg` 表达式
- 保持 template path 与 executor path 并存

## 4. motion formula helper

文件：

- `src/OpenVideoToolbox.Core/Execution/*Motion*Helper*.cs`
  - 具体命名在实现卡定

改动：

- 把 easing 对应的时间公式收敛到一个 helper
- 由 executor 调用 helper 生成 `ffmpeg` 表达式片段

第一期只支持：

1. `linear`
2. `ease_in`
3. `ease_out`
4. `ease_in_out`

目标：

- 避免 `move` / `orbit` / `zoompan` 各自复制一套 easing 文本
- 为后续新增 animation effect 保留复用点

## 5. still-image / overlay 运行时约束

文件：

- `src/OpenVideoToolbox.Core/Execution/FfmpegTimelineRenderCommandBuilder.cs`

改动：

- 在 motion effects 适配阶段显式约束第一版适用对象
- 优先建立在当前：
  - still-image loop 输入
  - overlay 合成
  - 单 clip filter chain

目标：

- 把第一期限制在当前 render baseline 已经覆盖的地带
- 不顺手演化成主轨复杂镜头系统

## 6. Core unit tests

文件：

- `src/OpenVideoToolbox.Core.Tests/BuiltInEffectCatalogTests.cs`
- `src/OpenVideoToolbox.Core.Tests/FfmpegTimelineRenderCommandBuilderTests.cs`
- 现有 timeline validator 相关测试文件
  - 优先补到已有 `V2TimelineSkeletonTests.cs`
  - 如测试量明显膨胀，再拆独立 `EditPlanValidatorEffectTests.cs`

改动：

### `BuiltInEffectCatalogTests`

- 断言 `move`
- 断言 `orbit`
- 断言 `zoompan`
- 断言三者 `category == animation`
- 断言 `templateMode` 预期应为 executor 路径

### `FfmpegTimelineRenderCommandBuilderTests`

- 新增 still-image `zoompan` 用例
- 新增 overlay `move` 用例
- 新增 overlay `orbit` 用例
- 断言 `render --preview` filter graph 包含稳定表达式片段

### validator tests

- 缺 `durationSeconds`
- 非法 `easing`
- 非法 scale / radius
- motion effect 被挂到第一版不支持的场景

## 7. CLI tests

文件：

- `src/OpenVideoToolbox.Cli.Tests/CommandArtifactsIntegrationTests.EffectsCommands.cs`

改动：

- 断言 `effects list` 可发现：
  - `move`
  - `orbit`
  - `zoompan`
- 断言 `effects describe move` / `orbit` / `zoompan`：
  - `templateMode == "executor"`
  - 参数 schema 稳定

说明：

- 第一版预计无需新增 CLI 参数
- 主要是 discovery / contract 层回归

## 8. render preview / contract 测试

文件：

- 现有 CLI render preview 测试文件
  - 具体落点在 implementation 卡里按现状选

改动：

- 至少增加一条最小 plan
- 覆盖：
  - motion effect 已被加载
  - preview 返回非空 `executionPreview`
  - command / filter graph 稳定

目标：

- 防止 unit test 通过、但 CLI contract 漂移

## 9. 文档同步

至少同步：

- `docs/roadmap.md`
- `docs/PROJECT_PROFILE.md`
- `src/OpenVideoToolbox.Core/Editing/MODULE.md`
- `src/OpenVideoToolbox.Core/Execution/MODULE.md`
- `src/OpenVideoToolbox.Cli/MODULE.md`

可选同步：

- `README.md`
  - 仅当对外功能矩阵明确增加 motion effects discoverability 或示例入口时

## 非目标

本计划明确不包含：

1. `fade_in` / `fade_out` / `crossfade` 的完整扩面
2. motion templates catalog
3. 页面 / 图表 / `.pptx` 后端
4. 角色绑定、口型、表情驱动
5. 关键帧编辑器或 UI 时间线
6. 第二套 animation schema
7. 新的 batch contract

## 测试清单

最小测试集：

1. `BuiltInEffectCatalogTests`
2. `FfmpegTimelineRenderCommandBuilderTests`
3. validator 相关单测
4. `CommandArtifactsIntegrationTests.EffectsCommands`
5. 至少一条 CLI render preview 相关测试
6. `dotnet build OpenVideoToolbox.sln`
7. `dotnet test OpenVideoToolbox.sln`

## 风险

## 1. validator 膨胀风险

如果这张卡试图一次性把所有 built-in effect 参数校验全补完，会把 implementation 范围明显拉大。

建议：

- 只补 motion effects 第一期开卡所需的最小参数校验
- 其余 built-in effect 保持现状，必要时留 TODO 到后续卡

## 2. execution owner 漂移风险

如果在 plan 或 CLI 侧直接塞原始 `ffmpeg` 表达式，会把 motion effect 语义分裂成：

- `Core.Execution` 一套
- 外部 `edit.json` 或 `Cli` 一套

这必须避免。

## 3. scope 漂移风险

如果在这张 implementation 卡里顺手补：

- `rotate`
- `slide_left/right`
- 更复杂 transition
- 自动主体检测

就会迅速偏离第一期范围。

## 回滚路径

若第一期 motion effects 最终效果或实现复杂度不符合预期，回滚范围应限制在：

1. built-in effect catalog 新增项
2. validator 的 motion effect 参数校验
3. render builder 的 motion executor 接线
4. motion-related unit / CLI tests
5. 本计划稿与相关 roadmap / v2 文档

不影响：

- 既有 `fade` / `dissolve` / `progress_bar`
- narrated-slides
- placeholder video baseline
- 现有 `effects` 命令 contract

## 当前结论

```text
阶段：motion effects plan
状态：done
目标：把第一期 motion effects 的实现顺序、文件落点和测试面拆清楚
范围：move / orbit / zoompan 的 built-in descriptor、validator、executor、preview、tests
本阶段不做：代码实现、模板扩面、角色动画、关键帧 UI、第二套 schema
前置依赖：motion effects spec 已完成
交付物：本计划稿
验收标准：owner、实现顺序、测试面、风险和回滚路径已明确
必须停下重判的情况：需要大量跨模块改造，或 motion effects 无法限制在 executor + single-object 范围内
当前结论：可进入 motion effects implementation，但当前仍不属于 E2 主线
下一步：如继续，只开 motion effects implementation
```
