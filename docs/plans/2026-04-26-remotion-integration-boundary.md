# Remotion 集成边界卡

最后更新：2026-04-26

## 目的

这份文档只回答一个问题：

> 当前仓库是否应该整合 `Remotion`，以及如果整合，边界应该画在哪里？

它不自动启动实现，也不把 `Remotion` 直接抬成当前主线目标。

这张卡只负责把下面几件事写清楚：

- `Remotion` 适不适合当前仓库
- 适合的话，应该以什么形态进入
- 哪些整合方式必须禁止
- 如果后续要继续，第一张 PoC 卡应该怎么开

## 当前结论

基于 **2026-04-26** 的当前判断，结论已经收敛为：

1. 当前放弃接入 `Remotion`。
2. 当前不进入 PoC。
3. 当前不进入 companion repo 实施。
4. 当前不进入仓库内桥接导出。
5. 当前只保留“为什么放弃”的边界记录，避免以后重复重判同一问题。

更准确地说：

- `Remotion` 不是当前仓库下一步
- `Remotion` 不是当前 `E2` 主线候选
- `Remotion` 不是当前 `v1.0` 边界的一部分
- `Remotion` 最多只保留为“未来若出现单一强场景，再重新开卡评估”的备选外部方案

更准确的定位应是：

- `export / bridge to Remotion`
- `external renderer adapter`
- `React-based motion template companion`

而不是：

- `OpenVideoToolbox` 的第二套正式渲染内核

## 为什么它曾经值得评估

基于 **2026-04-26** 的官方资料，`Remotion` 当前明确提供：

- 用 `React` 代码生成视频
- 本地与服务端渲染
- `@remotion/player` 做嵌入式预览
- `@remotion/renderer` 做服务端渲染

参考：

- 官网：[https://www.remotion.dev/](https://www.remotion.dev/)
- Renderer 文档：[https://www.remotion.dev/docs/renderer](https://www.remotion.dev/docs/renderer)
- Player 文档：[https://www.remotion.dev/docs/player](https://www.remotion.dev/docs/player)

这意味着它天然适合下面这类场景：

1. 数据驱动动态图文
2. 品牌片头 / 片尾
3. 代码生成型视觉模板
4. 需要组件化视觉表达，而不是传统剪辑轨道拼装的内容

这也是它最初值得被拿出来评估的原因。

但“值得评估”不等于“值得接入当前仓库”。

## 为什么不能直接并进当前主渲染路径

当前仓库的正式交付面仍是：

- `.NET`
- `CLI + Core`
- `ffmpeg / ffprobe`
- 确定性命令与结构化中间产物

而 `Remotion` 的核心前提是：

- `Node.js`
- `React`
- 另一套 composition / props / render runtime

如果直接深整合，会一次性引入这些变化：

1. 第二套运行时
   - 当前主仓库要从 `.NET + ffmpeg` 变成 `.NET + Node + React + ffmpeg`
2. 第二套渲染表达模型
   - 当前是 `edit.json` / timeline / ffmpeg builder
   - 新增后会变成 `edit.json` 与 Remotion composition 并存
3. 第二套依赖与发布问题
   - 安装
   - 版本兼容
   - CI
   - 发布资产
4. 第二套 owner 风险
   - 谁拥有最终 render 语义
   - 谁拥有模板运行时
   - 谁负责 debug 与错误映射

这已经不是“多接一个外部工具”这么简单，而是潜在地改写当前主产品边界。

## 为什么当前直接放弃

当前放弃，不是因为 `Remotion` 本身没价值，而是因为它对当前仓库来说，接入代价已经超过了当前收益。

### 代价判断

按当前仓库状态，接入成本至少会落在：

1. 第二套运行时成本
   - 需要同时维护 `.NET` 与 `Node / React`
2. 第二套渲染语义成本
   - 需要解释 `edit.json` 与 Remotion composition 的关系
3. 第二套工程与发布成本
   - CI
   - 依赖安装
   - 版本兼容
   - 资产发布
4. 第二套排障与支持成本
   - 用户环境问题
   - 渲染失败问题
   - license / 商业使用边界问题

### 当前收益判断

当前仓库最有价值、也最稀缺的能力，不是“再接一个代码生成视频框架”，而是：

1. `CLI + Core` 的确定性媒体工作流
2. `edit.json` / 模板 / batch / 外部 AI 编排
3. `ffmpeg` 可审计执行链
4. 收敛但可扩展的 `v2 timeline / effects`
5. 与外部专业工具的可互操作，而不是把它们都内嵌进来

把精力投入这些主线，比接一套新的 React 视频运行时更符合当前产品方向。

### 当前最终判断

因此，当前更合理的结论不是“先做个小 PoC 看看”，而是：

- 直接放弃接入
- 不为它重开当前主线
- 不为它新开 companion repo
- 不为它追加桥接命令
- 保留边界记录，结束本轮讨论

## 许可证与商业边界提醒

`Remotion` 不是一个可以在当前仓库里默认“顺手塞进来再说”的零边界依赖。

基于 **2026-04-26** 官方站点当前公开信息：

- 个人与最多 3 人公司可免费使用
- 更大的商业团队与自动化场景存在付费 license 边界

参考：

- 价格与授权入口：[https://www.remotion.dev/](https://www.remotion.dev/)
- License 文档：[https://www.remotion.dev/docs/license](https://www.remotion.dev/docs/license)

因此只要后续真的要整合，就必须把下面这件事先说清楚：

- 我们是在“研究外部适配可能性”
- 还是在“把 Remotion 作为正式产品依赖”

这两者的法律、分发和维护含义完全不同。

## 可接受的整合形态

如果未来某一天真的出现一个强烈、单一、无法用当前 `ffmpeg / timeline / effects` 低成本解决的场景，这三种形态仍是仅有的可接受方向。

但 **2026-04-26** 的当前结论是：本轮不采用其中任何一种。

### 形态 A：仓库外 companion repo

推荐程度：**最高**

建议形态：

```text
OpenVideoToolbox
  -> export deterministic payload
  -> companion remotion project consumes payload
  -> remotion render
```

优点：

- 不污染当前主仓库运行时
- 不改写 `Core.Execution` owner
- Node / React / license / hosting 问题单独处理
- 更适合先做 PoC

缺点：

- 用户需要维护两套项目
- 首轮集成体验不如“仓库内一体化”

### 形态 B：仓库内导出桥接，但 Remotion 运行时不内置

推荐程度：**中等**

建议形态：

- 当前仓库新增一个显式导出命令
- 只输出：
  - composition props
  - asset manifest
  - 最小 Remotion 项目骨架
- 真正渲染仍由外部 Node/Remotion 工程完成

更接近：

```text
ovt export-remotion --plan edit.json --output remotion-package/
```

而不是：

```text
ovt render --engine remotion
```

优点：

- 保持当前 CLI deterministic 边界
- 对外部 AI 仍是结构化、可审计的导出动作
- 可以直接复用当前仓库已有 plan / asset 管理能力

缺点：

- 仍需要设计新的导出 contract
- 需要明确 companion template 与 versioning 规则

### 形态 C：仓库内可选外部适配命令

推荐程度：**最低，但仍可接受**

形态类似：

- `doctor` 风格检测 Remotion / Node 环境
- `remotion-render` 风格命令显式调用外部 Node 项目

前提：

- 必须是显式命令
- 必须是 optional
- 必须保留完整 stdout / stderr / exit code
- 不能把当前 `render` 命令 silently 切换成 Remotion 后端

这条路只有在 companion repo 或桥接 contract 已收敛后才值得考虑。

## 不可接受的整合形态

以下做法当前默认禁止：

1. 直接把 `Remotion` 写进当前主 render path
   - 例如把 `render` 变成会自动选择 ffmpeg / Remotion 的黑盒命令
2. 在 `Core.Execution` 中直接内嵌 React / Node 运行时语义
3. 让 `Cli` 直接承担 Remotion composition 规则 owner
4. 在仓库内引入不可解释的脚本拼接逻辑，绕过当前结构化 envelope
5. 为了接 Remotion，重新发明第二套 plan 模型并与 `edit.json` 长期并行
6. 把 `Desktop` 提前变成 Remotion 编辑器或 React 视频编辑器壳
7. 在未明确 license 策略前，把 Remotion 写成默认正式依赖

## 推荐 owner 边界

如果后续真的要继续，建议 owner 保持如下：

- `OpenVideoToolbox.Core/Editing`
  - 继续拥有当前 `edit.json` / timeline / artifact 模型
- `OpenVideoToolbox.Core.Execution`
  - 继续拥有当前 ffmpeg canonical render 语义
- `OpenVideoToolbox.Cli`
  - 只允许新增显式导出 / 调用入口
  - 只负责参数解析、路径解析、结构化 envelope
- `Remotion companion layer`
  - 拥有 React composition
  - 拥有 Remotion props schema
  - 拥有 Node runtime / render pipeline

关键判断：

- `Remotion` 的 composition 语义不应回写成当前仓库新的长期 canonical owner
- 当前仓库最多只拥有“桥接到 Remotion”的 deterministic 接口

## 如果未来重开，第一阶段只允许做什么

如果后续要继续，第一阶段只允许做一个 **PoC 级桥接**，范围固定为：

1. 选一个最小场景
   - 例如品牌片头
   - 或讲解型动态图文页
2. 只定义一份最小输入 contract
   - `props.json`
   - `assets/`
   - `metadata.json`
3. 只验证一条链：
   - `OVT export -> Remotion consume -> render mp4`
4. 不改当前 `render`
5. 不改当前 `mix-audio`
6. 不改当前 `v1.0` 正式支持面

## 如果未来重开，推荐 PoC 命题

当前更合理的 PoC 命题不是：

- “把整个 OVT 都迁到 Remotion”

而是：

> 能否把一个当前不适合纯 ffmpeg/timeline 手工表达、但很适合 React 组件化表达的视觉模板，稳定导出到外部 Remotion 项目并成功渲染？

建议只选下面其中一个：

1. 品牌片头模板
2. 数据卡片 / 排行榜模板
3. 讲解型单页视觉模板

不要首轮就选：

1. 通用时间线编辑
2. 全量 `edit.json` parity
3. 复杂多轨视频剪辑
4. 当前所有模板自动转 Remotion

## 如果未来重开，建议输入输出协议方向

第一期 PoC 的协议建议尽量简单：

### OVT 输出

- `props.json`
  - 文本
  - 配色
  - 时间参数
  - 视觉参数
- `assets.json`
  - 图片 / 音频 / 视频路径
- `render-request.json`
  - fps
  - duration
  - width / height
  - output name

### Remotion 消费

- 读取上述 JSON
- 映射到固定 composition
- 输出 `.mp4`

避免第一期就做：

- 双向同步
- 任意模板编译
- 任意 React 组件远程加载
- 用户自定义脚本执行

## 需要显式停下的信号

出现以下任一信号时，不应继续在主仓库内推进，而应直接转 companion repo 或停止：

1. 需要在当前仓库内引入 Node 作为正式必需运行时
2. 需要让 `render` 的默认语义分裂成 ffmpeg / Remotion 双后端
3. 需要维护第二套长期 plan schema
4. 需要把 React composition 规则塞回 `Core`
5. 需要在 license / 分发策略上做长期承诺，但当前产品边界未决

当前这轮其实已经触发了其中多条信号，因此结论收敛为直接放弃，而不是继续试探式推进。

## 如果未来重开，建议测试要求

如果未来真的开 PoC 卡，最少要补这些验证：

1. 导出 contract snapshot
   - `props.json`
   - `assets.json`
   - `render-request.json`
2. CLI 导出命令 envelope 测试
3. 路径解析测试
4. 最小 companion render smoke
5. 一条失败路径测试
   - companion 缺依赖
   - composition id 不存在
   - 输入素材缺失

## 当前 stop-point

当前更合理的 stop-point 是：

1. 先只保留这份边界卡
2. 不进入实施
3. 不进入 PoC
4. 不把 Remotion 改挂为当前 roadmap 主线
5. 不修改当前 `v1.0` 发布边界
6. 回到“只做好自己的特色”主线

未来如果要重开，只允许按新明确卡片推进：

1. Remotion `spec`
2. Remotion `PoC plan`
3. Remotion `implementation`
4. Remotion `phase-check`

## 当前判定

```text
主题：Remotion integration
当前状态：abandoned_for_now
推荐结论：当前放弃接入，回到自身特色主线
禁止事项：不要把 Remotion 直接并进当前 canonical render path，也不要为它继续开当前阶段的 PoC
下一步：无；仅在未来出现新的单一强场景时再重新开卡评估
```
