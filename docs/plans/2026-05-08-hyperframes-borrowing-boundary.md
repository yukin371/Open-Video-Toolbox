# HyperFrames 借鉴边界卡

最后更新：2026-05-08

## 目的

这份文档只回答一个问题：

> `HyperFrames` 和当前仓库有哪些相似点，哪些能力值得借鉴，边界应该画在哪里？

它不自动启动实现，也不把 `HyperFrames` 直接抬成当前主线目标。

这张卡只负责把下面几件事写清楚：

- `HyperFrames` 解决的问题与当前仓库有哪些重合
- 哪些能力适合借鉴
- 哪些能力当前不该借
- 如果后续要继续，应该优先落在哪一层

## 当前结论

基于 **2026-05-08** 的当前判断，结论已经收敛为：

1. 当前不接入 `HyperFrames` 运行时。
2. 当前不引入 `HTML/CSS/JS` 作为新的主 authoring model。
3. 当前不引入浏览器渲染作为新的 canonical render path。
4. 当前只借鉴它在 `Studio`、诊断和 starter experience 上的做法。
5. 当前只把这些结论保留为未来 `D1` / `E2` 候选输入，不重开新的实施卡。

更准确地说：

- 值得借的是：
  - `Studio` 式工作台体验
  - render 前 lint / inspect 体验
  - starter project / catalog 体验
  - 可复现渲染环境的工程纪律
- 当前不该借的是：
  - `HTML/CSS/JS -> video` 的主表达模型
  - 浏览器帧捕获式主渲染路径
  - 围绕网页视觉模板再发明第二套长期计划模型

## 为什么它值得参考

基于 **2026-05-08** 可公开获取的官方资料，`HyperFrames` 当前明确强调这些能力：

- 面向 agent 的视频生成工作流
- 用 `HTML/CSS/JS` 做视频 composition
- 浏览器内预览与 `Studio`
- render 前检查与调试体验

参考：

- 官网：[https://hyperframes.app/](https://hyperframes.app/)
- 文档入口：[https://hyperframes.app/docs/1-startup/1-introduction](https://hyperframes.app/docs/1-startup/1-introduction)
- CLI / Studio 文档入口：[https://hyperframes.heygen.com/packages/cli](https://hyperframes.heygen.com/packages/cli)

这些能力之所以值得参考，不是因为当前仓库也应该改成网页视频框架，而是因为两者都在解决同一个上层问题：

- 让人或外部 AI 以稳定、可复现、可调试的方式产出视频

## 它与当前仓库真正相似的地方

两者的相似点主要在“工作流目标”，而不是“底层表达模型”。

共同点：

1. 都不是传统 NLE 的深度替代品
2. 都强调可编排、可复现、可审计
3. 都适合 agent / script 驱动
4. 都重视 preview、diagnostic 和开发者体验

但当前仓库的正式边界仍是：

- `.NET`
- `CLI + Core`
- `edit.json`
- `ffmpeg / ffprobe`
- 结构化输出与 batch 工作流

而 `HyperFrames` 的核心边界更接近：

- `HTML/CSS/JS`
- browser preview / studio
- browser-based render pipeline

所以它更像“可参考的产品体验样本”，而不是“可直接嵌入的同类运行时”。

## 为什么不能直接并进当前主路径

如果直接把 `HyperFrames` 式能力按原样并入当前仓库，至少会一次性引入这些变化：

1. 第二套 authoring model
   - 当前是 `edit.json`
   - 新增后会变成 `edit.json` 与 `HTML/CSS/JS composition` 并存
2. 第二套 render runtime
   - 当前是 `ffmpeg` / `ffprobe`
   - 新增后会变成浏览器渲染与当前执行链并存
3. 第二套调试与错误模型
   - DOM / layout / browser capture 失败语义与当前完全不同
4. 第二套长期 owner 风险
   - 谁拥有最终视频表达模型
   - 谁拥有 visual template runtime
   - `Desktop` 是否会被推成新的业务 owner

这已经超出了“借鉴功能体验”的范围，而是在改写当前仓库边界。

## 当前推荐借鉴清单

### Desktop 候选

这些最接近 `HyperFrames Studio` 的正向借鉴方式：

1. `Plan Inspector` 工作台
   - 可视化消费 `inspect-plan` / `validate-plan`
   - 展示素材缺失、slot 绑定、signals 状态和错误摘要
2. render 前预览面板
   - 统一展示 `render --preview` / `mix-audio --preview`
   - 展示输入素材、输出路径、execution preview 和关键 warning
3. 模板启动器
   - 图形化组织 `templates`、`scaffold-template`、`init-plan`、`init-narrated-plan`
4. 执行日志查看器
   - 汇总 stdout / stderr / exit code / produced paths
5. 只读式简化时间线视图
   - 先做 inspectable timeline
   - 当前不急着做复杂拖拽编辑

### Cli 候选

这些最适合沿当前命令边界继续增强：

1. 更强的 render 前诊断
   - 可保留为增强版 `validate-plan`
   - 或未来显式新增 `lint-plan`
2. 批量任务审计汇总
   - 汇总成功率、失败分类、耗时和缺依赖统计
3. starter / scaffold 体验增强
   - 继续加强 `commands.json`
   - 加强样例输入、目录骨架与说明文件
4. 可复现渲染环境摘要
   - 暴露依赖版本、模板来源、schemaVersion、plan hash 等审计字段
5. batch dry-run / diff
   - 在真实执行前统一看缺素材、风险和失败项

### Core 候选

这些最适合沉淀为稳定 owner，而不是停留在 CLI 文案：

1. 更细的计划诊断模型
   - `warning / error / suggestion / fix hint`
2. 更强的 preview 数据模型
   - 轨道摘要、片段摘要、时长统计
3. render 环境指纹
   - `ffmpeg / ffprobe` 版本
   - 模板来源
   - 计划摘要
4. 更丰富的 effect / template catalog 元数据
   - 参数说明
   - 使用约束
   - 示例
5. effect 使用限制诊断
   - 哪些 effect 只能用于 video clip
   - 哪些 effect 不能与 placeholder 混用

## 当前不建议借的能力

以下做法当前默认禁止：

1. 把 `HTML/CSS/JS` 改成当前仓库新的主编辑表达模型
2. 引入浏览器作为当前主 render backend
3. 让 `Desktop` 直接拼接 `ffmpeg` 或直接启动外部进程
4. 让 `Desktop` 持有独立于 `edit.json` 的 UI 私有计划格式
5. 为了模仿 `HyperFrames`，引入新的内置 AI provider 或黑盒自动编排层
6. 在当前阶段直接建设网页组件式视频 DSL、关键帧工作台或重交互动画编辑器

## 推荐落地顺序

如果未来真的要把这张卡转成实施卡，建议顺序固定为：

1. 先补 `Core / Cli` 的 plan 诊断与 render 前 lint
2. 再做 `Desktop` 的只读式 `inspect + validate + preview + logs` 工作台
3. 然后才做模板启动器与素材替换面板
4. 最后才评估是否需要简化时间线编辑

这个顺序的核心原因是：

- 先补稳定对象
- 再补 UI 壳
- 避免让 `Desktop` 倒逼内核边界漂移

## 推荐 owner 边界

如果后续真的继续，建议 owner 保持如下：

- `OpenVideoToolbox.Core/Editing`
  - 持续拥有 `edit.json`、timeline、inspection / diagnostic 相关稳定模型
- `OpenVideoToolbox.Core.Execution`
  - 持续拥有 render preview、execution summary 与环境指纹相关模型
- `OpenVideoToolbox.Cli`
  - 只负责命令入口、参数解析、结构化输出和 batch 汇总
- `OpenVideoToolbox.Desktop`
  - 只消费 `Core` / CLI 已稳定的边界对象
  - 不拥有第二套业务规则

关键判断：

- `HyperFrames` 值得借鉴的是产品体验，不是新的 canonical runtime
- 当前仓库最多只吸收它的壳层体验和工程纪律，不吸收它的主表达模型

## 如果未来重开，第一阶段只允许做什么

如果后续要继续，第一阶段只允许开一张“小边界、低风险”的候选卡，范围固定为下面其中一项：

1. `Core + Cli` 诊断增强卡
2. `Desktop` 只读式 `Plan Inspector` 工作台卡
3. 模板 starter / scaffold 体验增强卡

不要首轮就开：

1. 新视频 DSL
2. 浏览器渲染 PoC
3. 通用拖拽式时间线编辑器
4. 全量 Studio parity

## 当前 stop-point

当前更合理的 stop-point 是：

1. 先只保留这份借鉴边界卡
2. 不新增正式实施项
3. 不修改当前 `CLI + Core` 主边界
4. 不提前推动 `D1`
5. 未来只在 `Desktop` 重判或 `E2` 新卡出现时再引用这份文档

## 当前判定

```text
主题：HyperFrames borrowing boundary
当前状态：documented_as_reference_only
推荐结论：借 Studio / inspect / starter experience，不借 HTML-based 主 authoring/render model
禁止事项：不要把 HyperFrames 式运行时、浏览器渲染或第二套计划模型直接并入当前 canonical path
下一步：无；仅作为未来 D1 / E2 候选输入
```
