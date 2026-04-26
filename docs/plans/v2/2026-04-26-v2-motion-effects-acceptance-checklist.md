# V2 Motion Effects 验收清单

最后更新：2026-04-26

## 目的

这份清单只服务一件事：

> 让人工可以直接手动验证首期 `motion effects` 当前是否已经形成最小可接受闭环。

本清单只覆盖当前已落地范围：

- built-in motion effects：
  - `move`
  - `orbit`
  - `zoompan`
- `effects list/describe`
- `validate-plan` 对 motion effect 参数与使用场景的校验
- `render --preview` 对 motion effect 的真实消费

不覆盖：

- 插件 effect 加载
- track 级 motion effect
- placeholder visual motion
- 角色动画、rig、面部驱动或关键帧工作台

## 验收原则

本轮验收采用最小可重复样例：

1. 不依赖真实媒体文件
2. 只验证当前用户可见的 CLI 行为
3. 合法 motion 样例当前沿用：
   - `schemaVersion = 2`
   - `video` clip
   - 显式 `src`
   - 显式 `in/out`
   - clip 级 `move`

说明：

- 当前 render path 已支持 still-image motion 的 executor 路径
- 但当前 validator 的合法样例应沿用 `src + in/out` 这一形状，不以 `duration-only` 作为首期验收口径

建议在仓库根目录执行以下命令。

## 准备临时工作目录

```powershell
$root = Join-Path $env:TEMP ("ovt-motion-acceptance-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null
$root
```

记录输出目录，后续命令都复用它。

## Step 1：手测 `effects list`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  effects list `
  --json-out (Join-Path $root "effects-list.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "effects-list.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `command = "effects"`
  - `payload.mode = "list"`
- `payload.effects[].type` 至少包含：
  - `move`
  - `orbit`
  - `zoompan`

## Step 2：手测 `effects describe move`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  effects describe move `
  --json-out (Join-Path $root "effect-move.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "effect-move.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.mode = "describe"`
  - `payload.effect.type = "move"`
  - `payload.effect.category = "animation"`
  - `payload.effect.templateMode = "executor"`
- `payload.effect.parameters` 至少包含：
  - `fromX`
  - `fromY`
  - `toX`
  - `toY`
  - `durationSeconds`
  - `easing`

## Step 3：准备合法 motion plan

```powershell
@'
{
  "schemaVersion": 2,
  "source": { "inputPath": "unused-input.mp4" },
  "timeline": {
    "duration": "00:00:04",
    "resolution": { "w": 1920, "h": 1080 },
    "frameRate": 30,
    "tracks": [
      {
        "id": "still",
        "kind": "video",
        "clips": [
          {
            "id": "poster-001",
            "src": "poster.png",
            "start": "00:00:00",
            "in": "00:00:00",
            "out": "00:00:04",
            "effects": [
              {
                "type": "move",
                "fromX": "0",
                "fromY": "0",
                "toX": "120",
                "toY": "60",
                "durationSeconds": 4.0,
                "easing": "ease_in_out"
              }
            ]
          }
        ]
      }
    ]
  },
  "output": { "path": "final-motion.mp4", "container": "mp4" }
}
'@ | Set-Content -LiteralPath (Join-Path $root "motion.valid.json") -Encoding UTF8
```

通过标准：

- `$root\\motion.valid.json` 已生成

## Step 4：手测合法 motion plan 的 `validate-plan`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "motion.valid.json") `
  --json-out (Join-Path $root "motion.valid.envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "motion.valid.envelope.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.isValid = true`
  - `payload.checkMode = "basic"`
- `payload.issues` 为空数组

## Step 5：准备非法 easing plan

```powershell
@'
{
  "schemaVersion": 2,
  "source": { "inputPath": "unused-input.mp4" },
  "timeline": {
    "duration": "00:00:04",
    "resolution": { "w": 1920, "h": 1080 },
    "frameRate": 30,
    "tracks": [
      {
        "id": "still",
        "kind": "video",
        "clips": [
          {
            "id": "poster-001",
            "src": "poster.png",
            "start": "00:00:00",
            "in": "00:00:00",
            "out": "00:00:04",
            "effects": [
              {
                "type": "move",
                "fromX": "0",
                "fromY": "0",
                "toX": "120",
                "toY": "60",
                "durationSeconds": 4.0,
                "easing": "bogus"
              }
            ]
          }
        ]
      }
    ]
  },
  "output": { "path": "final-motion.mp4", "container": "mp4" }
}
'@ | Set-Content -LiteralPath (Join-Path $root "motion.invalid.json") -Encoding UTF8
```

通过标准：

- `$root\\motion.invalid.json` 已生成

## Step 6：手测非法 easing 的 `validate-plan`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "motion.invalid.json") `
  --json-out (Join-Path $root "motion.invalid.envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "motion.invalid.envelope.json")
```

通过标准：

- 命令退出码为非零或 envelope 标识 `payload.isValid = false`
- `payload.issues[*].code` 至少包含：
  - `timeline.effect.parameter.enum.invalid`

## Step 7：手测合法 motion plan 的 `render --preview`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "motion.valid.json") `
  --preview `
  --json-out (Join-Path $root "motion.preview.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "motion.preview.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `command = "render"`
  - `preview = true`
  - `payload.executionPreview.commandPlan.schemaVersion = 2`
- `payload.executionPreview.commandPlan.arguments` 包含：
  - `-filter_complex`
- filter graph 中包含 motion executor 生成片段，例如：
  - `color=c=black@0.0:s=1920x1080:r=30:d=4`
  - `overlay=x='((0)+(((120)-(0))*`

## 当前通过标准

通过本轮验收，至少要同时满足：

1. `move / orbit / zoompan` 都能在 `effects list` 中被发现
2. `effects describe move` 暴露 `templateMode = "executor"` 与稳定参数 schema
3. 合法 motion plan 在 `validate-plan` 下通过
4. 非法 easing plan 在 `validate-plan` 下返回结构化 issue
5. 合法 motion plan 在 `render --preview` 下生成真实 motion filter graph

## 当前不通过标准

出现以下任一情况，都不应视为通过：

1. `effects` discovery 缺少任一首期 motion effect
2. `describe` 与 validator / render 路径使用了不一致的参数语义
3. `validate-plan` 与 `render --preview` 对合法 motion plan 给出矛盾结论
4. preview 中 effect 被静默跳过
5. 验收过程中发现范围已开始滑向角色动画、关键帧平台或第二套动画系统

## 当前验收结论

本清单已于 `2026-04-26` 按上述步骤执行一次，当前结果为：

- Step 1：通过
- Step 2：通过
- Step 4：通过
- Step 6：通过
- Step 7：通过

关键结果：

- `effects list` 已确认包含 `move / orbit / zoompan`
- `effects describe move` 已确认 `templateMode = "executor"`
- 合法 motion plan 已确认可通过 `validate-plan`
- 非法 easing plan 已确认返回 `timeline.effect.parameter.enum.invalid`
- `render --preview` 已确认包含 motion executor 生成的 filter graph

结论：

```text
V2 motion effects: accepted
```
