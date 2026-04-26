# V2 简易 Motion Effects 边界卡

最后更新：2026-04-26

## 目的

这份文档只回答一个问题：

> 仓库是否应该承接“让单张图片人物动起来 / 多张图片动作过渡 / 固定轨迹小动效”这类需求？

结论不是“做完整动画系统”，而是把这类需求收敛成：

- `v2 timeline/effects` 下的窄范围能力增量
- 仅覆盖可参数化、可审计、可测试、可由 `ffmpeg` 稳定映射的简易 2D motion effect

它不是 `E2` 主线的新目标，也不是当前自动进入实施的活跃卡。它只是为后续是否重开一张窄卡，先把边界写清楚。

## 当前结论

当前建议：

- 可以做少量 **简易 deterministic motion effects**
- 不应把它命名或定位成 `live2d`
- 不应把仓库扩成角色动画器、关键帧动画工作台或第二套专业 NLE / puppet 系统

更准确的能力定位应是：

- `motion effects`
- `2d overlay animation`
- `timeline motion primitives`

## 为什么可以留在仓库内

当前仓库并不是完全没有这类基础。

已有能力包括：

- `schemaVersion = 2` 的 `timeline`
- built-in `effects` 描述符与参数 schema
- `render --preview` / `render` 对 timeline/effects 的真实消费
- still-image 输入 loop
- 多 video track 的 `overlay`
- 基础 transition / filter / layout effect

因此，“固定轨迹的小动效”本质上是：

- 扩展现有 `Core.Editing` effect catalog
- 扩展现有 `Core.Execution` 的 FFmpeg filter 映射

而不是新建第二套业务 owner。

## 为什么不能叫 Live2D

`Live2D` 在常见语境下通常意味着：

- 角色拆件
- 参数驱动变形
- 网格 / deformer
- 骨骼或部件联动
- 口型、眨眼、头部 / 身体姿态
- 需要独立动画工程、预览器或操控器

这些能力已经越过当前仓库边界，会把项目从：

- “CLI 媒体处理内核 + v2 timeline effects”

拉向：

- “角色动画系统 / 专业动画工具 / 第二套编辑器”

这不符合当前 roadmap，也不符合现有 owner 划分。

## 可做范围

只允许进入以下类型的能力：

1. 单素材内的简单运动
   - 平移
   - 缩放
   - 轻量旋转
   - 淡入淡出
2. 多素材之间的简单过渡
   - dissolve / fade
   - slide-like 过渡
   - 简单 crossfade
3. 明确轨迹的叠加层动画
   - 沿直线移动
   - 沿圆周或椭圆轨迹移动
   - 固定起终点的入场 / 出场
4. 适合 still-image 或 overlay asset 的轻量镜头感
   - `Ken Burns` 风格 `zoom + pan`
   - 固定锚点缩放
   - 带显式参数的画中画入场

## 不可做范围

以下能力默认不进入本仓库：

1. 角色骨骼、网格变形、deformer、布料或头发物理
2. 口型、眨眼、表情参数、摄像头驱动、语音驱动表演
3. 通用关键帧编辑器、曲线编辑器、动画 graph UI
4. 独立动画工程格式
5. 运行时 AI 驱动动画
6. 任何需要“像专业动画软件一样反复手调”的深度动画功能

一旦需求进入这些区域，应直接改用专业软件，再把结果回接到本仓库。

## 第一期开卡建议

如果后续要重开，只建议做一张窄卡，且范围固定为下列第一期 effect：

1. `move`
   - 作用：按起点 / 终点做线性位移
   - 典型用途：logo、贴图、立绘、截图卡片入场
2. `orbit`
   - 作用：围绕显式中心点做圆周或椭圆轨迹运动
   - 典型用途：某个物体围绕另一物体转圈
3. `zoompan`
   - 作用：针对 still-image 或 overlay 做固定时长的缩放平移
   - 典型用途：人物立绘、封面图、幻灯片视觉
4. `fade_in` / `fade_out`
   - 作用：作为 clip effect 或 transition 的轻量补充
   - 典型用途：简单出入场
5. `crossfade`
   - 作用：多张图 / overlay 间的最小过渡
   - 典型用途：图集切换、讲解型页面切换

其中：

- `move`、`orbit`、`zoompan` 才是这张卡的核心
- `fade_*`、`crossfade` 只是为了把样例工作流补完整

## 推荐 owner

- `OpenVideoToolbox.Core/Editing`
  - effect type 命名
  - 参数 schema
  - validator 规则
  - 说明文档与 discovery 输出
- `OpenVideoToolbox.Core/Execution`
  - effect 到 `ffmpeg` filter 的映射
  - preview / execute 的一致性
  - still-image / overlay 运行时拼接
- `OpenVideoToolbox.Cli`
  - `effects`
  - `validate-plan`
  - `render --preview`
  - 结构化输出透传

`Desktop` 不拥有这类规则，只能在未来消费稳定对象。

## 实施约束

后续如正式开工，必须同时满足以下约束：

1. effect 必须是确定性的显式参数
   - 不允许“自动猜镜头”
   - 不允许“自动让角色动起来”
2. effect 只通过 `edit.json` 表达
   - 不引入第二套动画文件格式
   - 不引入隐藏中间 DSL
3. `Cli` 不得持有效果规则
   - 只能透传
4. `render --preview` 与真实执行必须走同一条 builder 语义
5. 做不到稳定 `ffmpeg` 映射的效果，不进入第一期
6. 第一期开卡只做单 clip / 单 overlay 级能力
   - 不引入跨多个 clip 的复杂调度
   - 不引入时间线级自动布局器

## effect 参数方向建议

第一期 effect 参数建议尽量简单、显式：

### `move`

- `fromX`
- `fromY`
- `toX`
- `toY`
- `durationSeconds`
- `easing`

### `orbit`

- `centerX`
- `centerY`
- `radiusX`
- `radiusY`
- `startAngleDeg`
- `endAngleDeg`
- `durationSeconds`
- `easing`

### `zoompan`

- `fromScale`
- `toScale`
- `fromX`
- `fromY`
- `toX`
- `toY`
- `durationSeconds`
- `easing`

这类参数必须保持：

- 对 AI 可解释
- 对人工可手改
- 对 validator 可静态检查

## 测试要求

如果未来正式开工，最少要补这些测试：

1. `BuiltInEffectCatalog` 描述符测试
   - type / category / 参数 schema
2. `EditPlanValidator` 校验测试
   - 缺参数
   - 越界参数
   - 非法 easing / 轨迹参数
3. `FfmpegTimelineRenderCommandBuilder` filter graph 测试
   - `render --preview` filter graph 包含正确表达式
4. 最小样例 plan snapshot / command preview 测试
5. 至少一条 still-image / overlay 的手工 smoke

第一期不要把成功标准膨胀成：

- “生成效果看起来像动画软件”

第一期真正的成功标准应是：

- 参数稳定
- preview 正确
- 执行可回归
- 不破坏当前 owner 边界

## 必须停下并交给专业软件的信号

出现以下任一信号，就不应继续在仓库内追加：

1. 需要角色部件拆分与绑定
2. 需要面部 / 口型 / 表情参数
3. 需要可视化关键帧编辑和曲线编辑
4. 需要大量靠人工“调感觉”的动画工作
5. 需要复杂遮罩、形变、跟踪、粒子或物理系统

这时应改走：

- Live2D Cubism
- Adobe Character Animator
- DaVinci Resolve / Fusion
- Blender Grease Pencil 或类似工具

然后把导出结果回接到本仓库消费。

## 当前 stop-point

当前更合理的 stop-point 是：

- 先只保留这份边界卡
- 不自动进入实施
- 不修改当前 `E2` 主线
- 不把它挂成“现在就该做”的 roadmap 项

未来如果要继续，必须按“新明确卡片”方式重开，并且只允许按以下顺序推进：

1. motion effects `spec`
2. motion effects `plan`
3. motion effects `implementation`
4. motion effects `phase-check`

不要直接口头继续加功能。

## 当前判定

```text
阶段：V2 motion effects
当前状态：plan_done
建议动作：仍保持 stop-point；如后续真要做，下一张只能开 motion effects implementation，范围仍限于 move / orbit / zoompan
```
