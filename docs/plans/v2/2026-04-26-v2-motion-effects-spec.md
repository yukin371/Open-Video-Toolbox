# V2 Motion Effects Spec

> 状态：设计完成，待计划
> 依赖：`schemaVersion = 2` timeline、built-in effect catalog、timeline render baseline
> 前置文档：
> - [2026-04-26-v2-motion-effects-boundary.md](2026-04-26-v2-motion-effects-boundary.md)
> - [2026-04-24-template-effects-library-design.md](2026-04-24-template-effects-library-design.md)

## 1. 目的

这份文档只定义一件事：

> 如果后续正式重开 `motion effects`，第一期到底允许做什么，不允许做什么，以及这些 effect 应该怎样进入现有 `v2 timeline + effects + render` 路径。

这不是实现文档，也不是当前主线切换通知。

它只冻结：

- 第一期开卡范围
- effect 命名与参数方向
- `Core.Editing` / `Core.Execution` / `Cli` 的 owner 分工
- validator、preview、execute 的最小一致性要求
- 必须停下重判的条件

## 2. 当前结论

第一期只允许把 `motion effects` 做成：

- 现有 `TimelineEffect` 的 built-in effect 扩展
- 现有 `FfmpegTimelineRenderCommandBuilder` 的确定性 filter 映射扩展
- 现有 `effects` / `validate-plan` / `render --preview` 的稳定能力增量

第一期明确不做：

- `live2d`
- 角色动画
- 关键帧编辑器
- 第二套动画工程格式
- 第二套模板或页面系统

## 3. 第一期开卡范围

### 3.1 In Scope

第一期只允许以下三个核心 effect：

1. `move`
   - 单个 visual clip 或 overlay asset 的位移
2. `orbit`
   - 单个 overlay asset 围绕显式中心做圆周或椭圆轨迹运动
3. `zoompan`
   - still-image 或 overlay asset 的固定时长缩放平移

允许补充的样例配套 effect：

1. `fade_in`
2. `fade_out`
3. `crossfade`

但这些只允许作为：

- 样例工作流补全
- preview / render 测试样例补充

它们不应成为第一期的主实现重心。

### 3.2 Out of Scope

第一期明确不做：

1. 角色骨骼、网格、deformer、物理
2. 口型、眨眼、面部参数、语音驱动
3. 曲线编辑器、时间轴关键帧 UI、动画 graph
4. 自动镜头语言推断
5. 多对象自动避让、自动布局或复杂编排
6. `.pptx` / Markdown 页面渲染后端
7. 新的 batch contract 或第二套模板平台

## 4. 设计原则

### 4.1 显式参数优先

所有 effect 都必须由显式参数驱动。

不允许：

- “自动让角色动起来”
- “自动猜一个自然轨迹”
- “自动挑最舒服的镜头感”

### 4.2 描述符统一，执行分层

`Core.Editing` 只持有：

- type
- category
- 参数 schema
- 默认值
- validator 约束

`Core.Execution` 只持有：

- effect 到 `ffmpeg` filter 的映射
- preview / execute 的 builder 复用

`Cli` 只持有：

- discovery
- validate-plan 入口
- render preview / execute 入口

### 4.3 只扩现有 timeline，不造新模型

effect 必须仍然落在现有 `TimelineEffect` 上。

不允许为 motion effects 新增：

- 第二套 animation plan
- 第二套视觉脚本 DSL
- CLI 私有 effect 格式

### 4.4 先 still-image / overlay，后主轨复杂动画

第一期实现重心必须放在：

- still-image clip
- overlay asset
- 单对象轨迹

而不是：

- 多对象联动
- 多 clip 联动
- 复杂主轨镜头调度

## 5. owner 划分

### 5.1 Core.Editing

`OpenVideoToolbox.Core/Editing` owns：

- `move` / `orbit` / `zoompan` 的 type 命名
- 参数 schema
- 默认值
- 合法参数区间
- `easing` 允许值
- 适用对象边界
- built-in effect discovery 输出
- `validate-plan` 识别规则

### 5.2 Core.Execution

`OpenVideoToolbox.Core/Execution` owns：

- `ffmpeg` filter graph 映射
- still-image / overlay 运行时接线
- preview / execute 一致性
- 如模板不足时的 executor fallback

### 5.3 Cli

`OpenVideoToolbox.Cli` owns：

- `effects list/describe` 的发现输出
- `validate-plan` 的命令入口
- `render --preview` 的结果透传

CLI 不得拥有：

- motion effect 参数含义
- easing 解释
- 轨迹计算逻辑

## 6. effect 语义

## 6.1 `move`

### 目标

在固定时长内，把一个 visual 从起点移动到终点。

### 第一版适用对象

- still-image clip
- overlay asset
- 轻量 visual layer

第一版不保证：

- 任意视频主轨的复杂裁切运动
- 多对象联动 move

### 参数方向

```jsonc
{
  "type": "move",
  "category": "animation",
  "parameters": {
    "fromX": { "type": "string", "required": true },
    "fromY": { "type": "string", "required": true },
    "toX": { "type": "string", "required": true },
    "toY": { "type": "string", "required": true },
    "durationSeconds": { "type": "float", "required": true, "min": 0.1 },
    "easing": {
      "type": "string",
      "required": false,
      "default": "linear",
      "enum": ["linear", "ease_in", "ease_out", "ease_in_out"]
    }
  }
}
```

### 备注

- 坐标第一版建议使用字符串表达式，以兼容 `ffmpeg` 表达式与后续相对定位
- 第一版不引入百分比 / 像素双制切换开关，避免 schema 膨胀

## 6.2 `orbit`

### 目标

让一个 overlay asset 围绕显式中心点做圆周或椭圆轨迹运动。

### 第一版适用对象

- logo
- 装饰元素
- 讲解型视觉中的轻量提示元素

第一版不保证：

- 多对象联动公转
- 自动绕任意其他对象的真实边界做轨道计算

### 参数方向

```jsonc
{
  "type": "orbit",
  "category": "animation",
  "parameters": {
    "centerX": { "type": "string", "required": true },
    "centerY": { "type": "string", "required": true },
    "radiusX": { "type": "float", "required": true, "min": 0.0 },
    "radiusY": { "type": "float", "required": true, "min": 0.0 },
    "startAngleDeg": { "type": "float", "required": false, "default": 0.0 },
    "endAngleDeg": { "type": "float", "required": false, "default": 360.0 },
    "durationSeconds": { "type": "float", "required": true, "min": 0.1 },
    "easing": {
      "type": "string",
      "required": false,
      "default": "linear",
      "enum": ["linear", "ease_in", "ease_out", "ease_in_out"]
    }
  }
}
```

### 备注

- `radiusX != radiusY` 时允许椭圆轨迹
- 第一版不引入自动旋转朝向，不做“沿路径自动转头”

## 6.3 `zoompan`

### 目标

为 still-image 或 overlay 提供固定时长的缩放平移镜头感。

### 第一版适用对象

- 立绘
- 封面图
- 幻灯片图片
- 文档截图或页面截图

### 参数方向

```jsonc
{
  "type": "zoompan",
  "category": "animation",
  "parameters": {
    "fromScale": { "type": "float", "required": true, "min": 0.1 },
    "toScale": { "type": "float", "required": true, "min": 0.1 },
    "fromX": { "type": "string", "required": true },
    "fromY": { "type": "string", "required": true },
    "toX": { "type": "string", "required": true },
    "toY": { "type": "string", "required": true },
    "durationSeconds": { "type": "float", "required": true, "min": 0.1 },
    "easing": {
      "type": "string",
      "required": false,
      "default": "linear",
      "enum": ["linear", "ease_in", "ease_out", "ease_in_out"]
    }
  }
}
```

### 备注

- 第一版优先服务 still-image 路径
- 不引入自动人脸 / 主体检测
- 不把它扩成智能镜头生成器

## 7. easing 规则

第一期只允许四种 easing：

1. `linear`
2. `ease_in`
3. `ease_out`
4. `ease_in_out`

这样做的原因：

- 对 AI 与人工都容易理解
- validator 易于锁定
- render 映射可控

第一期不允许：

- 自定义贝塞尔曲线
- 多段 easing
- 弹性 / 回弹 / 物理型 easing

## 8. render 映射策略

## 8.1 第一版目标

第一版只要求：

- `effects describe` 能稳定发现
- `validate-plan` 能识别并拦截非法参数
- `render --preview` 能生成稳定的 filter graph
- `render` 能复用同一映射执行

第一版不要求：

- 所有 effect 都只靠单条简单模板即可表达

## 8.2 模板优先，必要时 executor fallback

如能通过现有 `FfmpegFilterTemplateSet.Filters` 模板稳定表达，应优先走模板。

如出现以下情况，可允许进入 executor fallback：

1. 需要在 filter 表达式中注入按时间计算的位置公式
2. 需要围绕中心点计算 `x/y`
3. 需要比当前简单占位符模板更复杂的表达式拼装

即便走 executor fallback，也必须满足：

- effect 对外仍然只表现为 built-in descriptor
- `Cli` 看不到第二套内部实现细节

## 8.3 overlay 复用现有路径

当前 render baseline 已支持多 video track `overlay=eof_action=pass`。

因此第一期 motion effects 应优先建立在：

- 现有 overlay 合成
- still-image loop 输入
- 单 clip filter chain

而不是另造新的合成分支。

## 9. validator 规则

第一期至少需要以下静态校验：

1. 必填参数缺失
2. `durationSeconds <= 0`
3. `fromScale` / `toScale <= 0`
4. `radiusX` / `radiusY < 0`
5. 非法 `easing`
6. effect 被挂到第一版不支持的对象类型上

第一版 validator 应优先防止：

- 参数空缺
- 明显数值越界
- 使用场景超出第一期范围

而不是追求一开始就做复杂几何正确性验证。

## 10. 对 `effects` 命令的要求

一旦进入实现，`effects list/describe` 至少要能稳定表达：

1. `type`
2. `category`
3. `displayName`
4. 参数 schema
5. 默认值
6. 第一版适用说明

不要让外部 AI 只能从代码里猜 `move` / `orbit` / `zoompan` 的入参。

## 11. 对模板与示例的要求

第一期不要求立即把 motion effects 接到所有模板。

但至少应提供：

1. 一条 still-image `zoompan` 示例
2. 一条 overlay `move` 示例
3. 一条 overlay `orbit` 示例

这些示例可以先落在：

- preview plan 示例
- 测试样例
- 单独 acceptance checklist

第一期不要把目标膨胀成：

- 批量 motion template catalog
- 页面动画模板库

## 12. 测试要求

如果后续进入实现，最低测试面固定为：

1. `BuiltInEffectCatalogTests`
   - type / category / 参数 schema / 默认值
2. `EditPlanValidator` 测试
   - 非法 easing
   - 缺参数
   - 非法时长
   - 非法半径 / 缩放
3. `FfmpegTimelineRenderCommandBuilderTests`
   - `move` filter graph
   - `orbit` filter graph
   - `zoompan` filter graph
4. `render --preview` 相关 command snapshot 或等价 contract 测试
5. 至少一条 still-image / overlay 的手工 smoke

## 13. 验收标准

`motion effects spec` 这一阶段完成的标准是：

1. 第一期 effect 名单冻结
2. 非目标明确
3. 参数方向明确
4. owner 明确
5. render 映射策略明确
6. validator 最小责任明确
7. 测试面明确

它不要求：

1. 已有代码实现
2. 已经选定最终 `ffmpeg` 公式细节
3. 已经接入模板目录

## 14. 必须停下重判的情况

出现以下任一情况，就不应直接进入 implementation，而应先新开 plan 或重新评估：

1. 需要新增独立 animation schema
2. 需要 `Cli` 持有效果规则
3. 需要大量特殊 case 才能映射 `ffmpeg`
4. 需要做角色绑定、口型或表情驱动
5. 需要 UI 级关键帧编辑
6. 第一期 effect 无法限制在单对象 / still-image / overlay 范围内

## 15. 当前结论

```text
阶段：motion effects spec
状态：done
目标：冻结第一期 effect 规格，而不是直接实现
范围：move / orbit / zoompan
本阶段不做：代码实现、模板扩面、角色动画、关键帧 UI
前置依赖：v2 timeline render baseline 已存在
交付物：本 spec 文档
验收标准：effect 名单、参数方向、owner、测试面已明确
必须停下重判的情况：需要第二套动画系统或 scope 超出单对象 motion primitive
当前结论：可进入 motion effects plan，但当前仍不属于 E2 主线
下一步：如继续，只开 motion effects plan
```
