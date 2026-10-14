# E2 窄卡：v1.0 Release Note / Changelog 收口

最后更新：2026-04-26

## 目的

这张卡只做一件事：

> 把当前已经在 `v1.0` 边界卡里收敛出来的正式承诺能力、已知限制和放行口径，同步到仓库的 canonical release note / changelog 入口。

它不创建 `v1.0.0` tag，也不直接发布。

它只负责把下面三件事做干净：

1. `CHANGELOG.md` 不再停留在过时的宽泛描述
2. `v1.0` 的正式支持面与附带能力有统一口径
3. 后续真正发版时，不需要重新从零拼 release note

## 为什么这是当前主线的合理下一步

当前主线已经不是“继续补功能”，而是：

- 把 `CLI + Core` 的稳定交付面收成可发布口径
- 避免 `v1.0` 到真正发版时还在临时解释支持范围

`docs/plans/v1.0/2026-04-26-v1-0-release-precheck-and-boundary.md` 已经把：

- 正式承诺能力
- 非主阻塞能力
- 已知限制
- go / no-go 结论模板

都写清楚了。

下一步自然不该再补新的边界卡，而应把这些结论同步到：

- `CHANGELOG.md`

否则真正进入 release 动作时，最容易出现：

1. 边界卡说一套，changelog 写另一套
2. 附带能力被误写成主承诺
3. 已知限制漏写，导致后续用户误解为实现故障

## 本卡范围

只允许做以下事项：

1. 同步 `CHANGELOG.md`
   - 正式支持面
   - 附带能力
   - 已知限制基线
2. 明确 `Unreleased` 口径
   - 当前不是已经发布 `1.0.0`
   - 只是为下一次 release 准备 canonical 文案
3. 保证 changelog 与 `v1.0` 边界卡不冲突

## 本卡不做

1. 不创建 release tag
2. 不修改版本号
3. 不重写用户功能文档全量内容
4. 不新增代码实现
5. 不重新设计 `v1.0` 边界

## 建议同步原则

### 正式承诺能力

应明确写成：

- `CLI + Core`
- `doctor`
- `probe / plan / run`
- `templates / init-plan / scaffold-template / validate-plan / render`
- `inspect-plan / attach-plan-material / replace-plan-material / bind-voice-track`
- `scaffold-template-batch / render-batch`
- `mix-audio --preview` / `mix-audio`

### 附带但非主承诺能力

应明确降级为：

- `init-narrated-plan`
- `effects`
- `export --format edl`
- `schemaVersion = 2`
- `transcribe` / `separate-audio`

### 已知限制

至少必须写清：

1. `Desktop` 不在当前正式支持面
2. `valid draft != directly renderable output`
3. `subtitles.mode = sidecar` 当前不是烧录
4. optional dependency commands 不应视为默认环境硬承诺

## 完成标准

只有同时满足以下条件，这张卡才算完成：

1. `CHANGELOG.md` 已可直接作为 release note 基线使用
2. changelog 与 `v1.0` 边界卡口径一致
3. 没有把附带能力误写成正式承诺
4. 没有让读者误以为 `1.0.0` 已经发布

## stop-point

本卡完成后就应停止，不继续扩成：

- 最终发版动作
- 更大范围的文档重写
- 新一轮功能或架构讨论

```text
阶段：E2 release discipline
当前状态：narrow_card
建议动作：只同步 changelog / release note 口径，完成后停止
```
