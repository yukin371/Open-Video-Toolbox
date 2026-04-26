# v1.0 发布前预检测与边界整理

最后更新：2026-04-26

## 目的

这份文档只回答三个问题：

1. `v1.0` 到底承诺什么，不承诺什么
2. 发布前应该验证哪些能力，分别怎么验证
3. 哪些缺陷会阻塞 `v1.0`，哪些缺陷可以带着说明发布

它不改当前版本号，也不自动触发 `1.0.0`。截至 **2026-04-26**，仓库公开版本基线仍是 `0.1.0`。

## 为什么现在先写这张卡

- 当前仓库已经具备真实发布链，但“什么时候算可以叫 `v1.0`”仍未被写成明确门槛。
- `D1`、`V2`、简易 `motion effects`、`narrated` 都在仓库里有存在感，如果不先收敛 `v1.0` 边界，很容易在版本定义上继续漂移。
- `1.0.0` 一旦发布，就不应再靠“以后再解释”来定义支持面。

## v1.0 的建议定位

`v1.0` 建议定义为：

> 一个以 `CLI + Core` 为正式交付面的稳定发布版，能够让个人工作者、脚本和外部 AI 代理，以确定性方式完成常见视频处理与模板化生产闭环。

它不等于：

- 专业 NLE
- Desktop 正式版
- 通用时间线编辑器
- 角色动画器 / `Live2D` / 关键帧工作台
- 内置 AI provider 产品

## v1.0 正式边界

### 必须明确承诺的能力

`v1.0` 至少应正式承诺以下能力面：

1. 安装与运行基线
   - GitHub Release 单文件发布可获取、可运行
   - `doctor` 能明确 required / optional 依赖状态
2. 传统 CLI 媒体处理基线
   - `probe`
   - `plan`
   - `run`
   - `cut` / `concat` / `extract-audio`
   - `audio-analyze` / `audio-gain` / `audio-normalize`
3. 模板化编辑主闭环
   - `templates`
   - `init-plan` / `scaffold-template`
   - `validate-plan`
   - `render`
   - `mix-audio`
4. `edit.json` 计划维护闭环
   - `inspect-plan`
   - `replace-plan-material`
   - `attach-plan-material`
   - `bind-voice-track`
5. 批量工作流最小闭环
   - `scaffold-template-batch`
   - `render-batch`
   - 相关 `summary.json` / `results/<id>.json` 约定稳定
6. 发布与可审计基线
   - 关键命令成功 / 失败路径有稳定结构化 envelope
   - `--json-out` 与 stdout 语义一致
   - 当前 release workflow 可重复产出跨平台 single-file 资产

### 可以随版本一起存在，但不应定义为 v1.0 主承诺的能力

这些能力可以继续保留在仓库和发布物里，但不建议拿它们定义 `v1.0` 是否成立：

- `init-narrated-plan` / `init-narrated-plan-batch`
- `effects` 与当前受限 `move / orbit / zoompan`
- `export --format edl`
- `transcribe` / `separate-audio` 这类强依赖外部可选工具的链路
- `schemaVersion = 2` 的持续孵化能力

建议口径：

- 它们可以作为“已附带能力”出现
- 但在 `v1.0` 发布判断中应视为“非主阻塞项”
- 如果其文档说明仍不够清晰，应在 release note 里单独标记为受限能力或孵化能力

### 明确不进入 v1.0 承诺的能力

- Desktop 正式交互产品
- 复杂时间线编辑
- 专业调色、复杂转场编排、关键帧系统
- 插件运行时代码执行
- 仓库内 AI provider / 远程模型集成
- 复杂角色动画、`Live2D`、骨骼绑定、表情驱动

## v1.0 放行前必须回答的问题

```text
1. 当前是否已经有一组稳定、高频、可文档化的 CLI 主工作流？
2. 当前是否已经能区分“正式承诺能力”和“附带但受限能力”？
3. 当前 release 资产与文档是否足够让陌生用户在新环境上复现核心能力？
4. 当前已知缺陷是否都能被归类为：阻塞 / 非阻塞 / 文档已声明？
```

任一问题回答不清，不建议进入 `1.0.0`。

## 预检测清单

### A. 发布资产与环境预检

| 项目 | 预期结果 | 建议验证方式 | 阻塞级别 |
| --- | --- | --- | --- |
| GitHub Release 资产齐全 | 至少有当前目标平台 single-file 资产 | 检查 `release.yml`、tag 产物、release 页面 | 阻塞 |
| 程序可在新环境启动 | `ovt --help`、`doctor` 可运行 | 在干净环境执行 smoke | 阻塞 |
| 必需依赖解释清晰 | `ffmpeg` / `ffprobe` 缺失时 `doctor` 说明明确 | 人工 smoke + JSON 输出检查 | 阻塞 |
| 可选依赖解释清晰 | `whisper-cli` / `demucs` 状态不混淆为主故障 | 人工 smoke + JSON 输出检查 | 非阻塞 |

### B. CLI 契约稳定性

| 项目 | 预期结果 | 建议验证方式 | 阻塞级别 |
| --- | --- | --- | --- |
| 关键命令 envelope 稳定 | 顶层 `{ command, preview, payload }` 不漂移 | 现有 snapshot / CLI tests | 阻塞 |
| `--json-out` 与 stdout 一致 | 无语义分叉 | 现有 tests + spot check | 阻塞 |
| 失败路径仍结构化 | 不退回纯 usage 文本 | 失败路径 tests / smoke | 阻塞 |
| exit code 语义稳定 | 已发布语义不漂移 | CLI tests + release note 复核 | 阻塞 |

### C. v1 主工作流能力验证

| 工作流 | 通过标准 | 建议验证方式 | 阻塞级别 |
| --- | --- | --- | --- |
| `probe -> plan -> run` | 能从单输入稳定完成传统转码链路 | 自动化测试 + 最小真实 smoke | 阻塞 |
| `templates -> init-plan -> validate-plan -> render` | 能稳定生成并消费 `edit.json` | 自动化测试 + 最小真实 smoke | 阻塞 |
| `scaffold-template -> inspect-plan -> attach/replace -> render` | 能完成 plan 二次维护闭环 | 自动化测试 + 最小真实 smoke | 阻塞 |
| `scaffold-template-batch -> render-batch` | 能稳定写出 batch summary / results | 自动化测试 + 人工抽查文件输出 | 阻塞 |
| `mix-audio --preview` / `render --preview` | preview 结构与执行链一致，可审计 | 自动化测试 | 阻塞 |
| `doctor` | required / optional 依赖区分稳定 | 自动化测试 + 人工 smoke | 阻塞 |

### D. 非主阻塞能力抽检

| 能力 | 目标 | 建议验证方式 | 默认级别 |
| --- | --- | --- | --- |
| `transcribe` | 若环境具备依赖，可生成结构化结果 | 可选 real smoke | 非阻塞 |
| `separate-audio` | 若环境具备依赖，可生成 stems 结果 | 可选 real smoke | 非阻塞 |
| `init-narrated-plan` | 现有 accepted 范围仍可跑通 | 复用现有 acceptance checklist | 非阻塞 |
| `effects` / `move / orbit / zoompan` | 当前受限 executor path 不崩溃 | 复用现有 phase-check / acceptance checklist | 非阻塞 |
| `export --format edl` | v1 / v2 最小导出仍成立 | 自动化测试 + spot check | 非阻塞 |

## 缺陷分级规则

### 一票否决缺陷

出现以下任一问题，不建议发布 `v1.0`：

1. 已承诺主工作流无法在受支持环境中重复跑通。
2. 关键命令 JSON 契约出现 breaking drift。
3. 失败路径退回纯 usage 或吞掉关键错误上下文。
4. release 资产无法下载、运行，或版本与文档不一致。
5. 文档把不在边界内的能力错误宣传为正式支持面。

### 可带说明发布的缺陷

以下问题默认不必阻塞 `v1.0`，前提是 release note 或文档已声明清楚：

1. 可选依赖相关命令在无依赖环境下不可用，但 `doctor` 和错误信息清晰。
2. `narrated`、`effects`、`export` 等非主承诺能力仍有受限边界，但边界被明确标注。
3. 包管理器渠道未全部铺开，只要 canonical GitHub Release 资产稳定可用。
4. Desktop 仍未启动或仅有 placeholder。

## 建议执行顺序

1. 先验发布资产
   - release tag
   - 平台资产
   - 版本号 / 文档一致性
2. 再验契约稳定性
   - snapshot
   - CLI tests
   - `--json-out` 与失败路径
3. 再验主工作流
   - 单项 smoke
   - batch smoke
4. 最后处理非主阻塞能力
   - optional dependency commands
   - `narrated`
   - `effects`
   - `export`

## 建议执行表模板

| 类别 | 条目 | 执行命令 / 入口 | 结果 | 缺陷级别 | 备注 |
| --- | --- | --- | --- | --- | --- |
| 资产 | release 资产完整性 |  |  |  |  |
| 契约 | CLI snapshot / JSON contract |  |  |  |  |
| 主工作流 | `probe -> plan -> run` |  |  |  |  |
| 主工作流 | `templates -> init-plan -> validate-plan -> render` |  |  |  |  |
| 主工作流 | `scaffold-template -> inspect/attach/replace -> render` |  |  |  |  |
| 主工作流 | `scaffold-template-batch -> render-batch` |  |  |  |  |
| 非主阻塞 | `transcribe` / `separate-audio` |  |  |  |  |
| 非主阻塞 | `narrated` / `effects` / `export` |  |  |  |  |

## 当前基线（2026-04-26）

- 版本与发布链基线：
  当前仓库正式版本基线仍是 `0.1.0`。`src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj` 仍声明 `AssemblyName = ovt`、`VersionPrefix = 0.1.0`；本地仓库当前可见 tag 为 `v0.1.0`。`.github/workflows/release.yml` 仍维持 tag 触发的跨平台 self-contained single-file 发布，当前目标平台为 `win-x64`、`linux-x64`、`osx-x64`，其中 Windows 额外导出 `ovt-win-x64.exe` 便携资产。
- 正式承诺面候选：
  按当前 `README.md`、`docs/FEATURES_AND_USAGE.md`、`docs/roadmap.md` 与 `docs/PROJECT_PROFILE.md`，仓库已经具备一组可以作为 `v1.0` 正式承诺面的 CLI 主工作流：传统 `probe -> plan -> run`、模板 `templates -> init-plan / scaffold-template -> validate-plan -> render`、plan 维护闭环 `inspect / replace / attach / bind-voice-track`、以及 batch `scaffold-template-batch / render-batch`。
- 非主阻塞能力状态：
  `init-narrated-plan`、`init-narrated-plan-batch`、`effects`、`export --format edl`、`schemaVersion = 2` timeline 能力当前都已在仓库中存在并有阶段验收材料，但更合理的口径仍是“已附带的受限能力 / 孵化能力”，而不是 `v1.0` 是否成立的核心放行项。
- 明确不纳入正式承诺：
  `Desktop` 当前仍是 placeholder + 候选阶段，不进入 `v1.0` 正式支持面；复杂时间线编辑、角色动画、`Live2D`、关键帧工作台也不在当前边界内。
- 当前阶段判断：
  以仓库能力面看，当前已经接近“可定义 `v1.0` 边界”的状态；但以发布放行证据看，当前仍不能直接得出“已经 ready for 1.0.0”。主要原因不是主功能缺失，而是这张卡下的 release 预检测尚未完整走完，尤其是干净环境启动、主工作流真实 smoke、以及发布物视角的一致性复核还没有形成集中证据包。

## 当前预验证（2026-04-26）

- 解决方案构建：
  `dotnet build .\OpenVideoToolbox.sln` 已在本轮文档收敛期间通过，当前仓库处于可构建状态。
- 全量测试：
  `dotnet test .\OpenVideoToolbox.sln` 当前通过，结果为：
  - `OpenVideoToolbox.Core.Tests`：`193 passed`
  - `OpenVideoToolbox.Cli.Tests`：`195 passed`
  - 合计：`388 passed`
- 当前机器依赖基线：
  `dotnet run --project .\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- doctor` 当前返回 `isHealthy = true`、`missingRequiredCount = 0`、`missingOptionalCount = 3`。这说明本机 required dependency `ffmpeg` / `ffprobe` 已可用；缺失项集中在 optional dependency：`whisper-cli`、`demucs`、`whisper-model`。
- 近干净环境启动样本：
  本轮已在 **2026-04-26** 用 fresh `dotnet publish` 产出的 `win-x64` single-file `ovt.exe`，从独立临时目录直接执行 `--help` 与 `doctor --json-out`，且未设置 `OVT_WHISPER_CLI_PATH`、`OVT_WHISPER_MODEL_PATH`、`OVT_DEMUCS_PATH`。结果如下：
  - `ovt.exe --help`：通过
  - `ovt.exe doctor --json-out <doctor.json>`：通过
  - stdout 与 `doctor.json`：一致
  - `doctor` 返回：`isHealthy = true`、`missingRequiredCount = 0`、`missingOptionalCount = 3`
  这说明当前发布物不依赖仓库工作目录或本轮额外 `OVT_*` 环境变量，即可在接近陌生用户的最小样本下正常启动并输出稳定结果。
- 当前可直接支持的发布口径：
  从这次 `doctor` 样本看，当前机器可以直接支撑不依赖 optional tooling 的 `v1` 主工作流验证；但不能用它来证明 `transcribe` / `separate-audio` 已在默认环境可用，因此这两类命令仍应继续按“非主阻塞 + 需额外依赖”口径处理。
- release 形态一致性：
  当前仓库内版本号、程序集名、本地 tag 与 release workflow 口径一致，没有看到“文档说一套、发布脚本做另一套”的直接冲突信号。
- GitHub Release 直检：
  本轮已在 **2026-04-26** 直接复核仓库 GitHub Release 信息：
  - 当前 latest tag：`v0.1.0`
  - 发布时间：`2026-04-24T02:50:22Z`
  - `draft = false`
  - `prerelease = false`
  - 已确认上传资产：
    - `ovt-linux-x64.tar.gz`
    - `ovt-osx-x64.tar.gz`
    - `ovt-win-x64.exe`
    - `ovt-win-x64.zip`
  说明：
  - GitHub Release 页面显示的 `Assets 6` 包含 GitHub 自动附带的 source archive 统计口径；通过 GitHub API 复核后，当前 maintainer 实际上传资产数为 `4`，与 release workflow 的目标产物口径一致。
- 发布物视角 smoke：
  本轮已额外用 `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` 重新生成当前代码面的本地 single-file `ovt.exe`，并基于 `ffmpeg` 生成的 1 秒最小样例做了一轮 release-view smoke。
  - `ovt.exe doctor`：通过
  - `ovt.exe probe <input>`：通过
  - `ovt.exe plan <input>`：通过
  - `ovt.exe run <input>`：通过，成功写出 `smoke-output.mp4`
  - `ovt.exe scaffold-template <input> --template shorts-captioned --dir <workdir> --validate`：通过，成功写出 `edit.json`、`commands.json`
  - `ovt.exe init-plan <input> --template shorts-captioned ...`：通过，但默认生成的是 `clips = []` 的有效草稿
  - `ovt.exe validate-plan --plan <bare-init-plan>`：通过
  - `ovt.exe render --plan <bare-init-plan> --preview`：失败，错误为 `Render plan must contain at least one clip`
  - `ovt.exe init-plan ... --transcript <transcript.json> --seed-from-transcript`：通过，生成含真实 clip 的 seeded plan
  - `ovt.exe render --plan <seeded-plan> --preview`：通过
  - `ovt.exe render --plan <seeded-plan> --overwrite`：通过，成功写出 `seeded-final.mp4`
  - `ovt.exe inspect-plan --plan <seeded-plan> --check-files`：通过
  - `ovt.exe attach-plan-material --plan <seeded-plan> --subtitles --path <captions-a.srt> --subtitle-mode sidecar --check-files`：通过
  - `ovt.exe replace-plan-material --plan <seeded-plan> --subtitles --path <captions-b.srt> --subtitle-mode sidecar --check-files`：通过
  - `ovt.exe render --plan <seeded-plan-with-sidecar-subtitles> --overwrite`：通过，成功保留成片输出并额外写出 sidecar `seeded-final.srt`
  - `ovt.exe bind-voice-track --plan <edit.json> --path <dub.wav> --path-style relative --check-files`：通过，默认成功建立 `voice-main` / `voice` 轨并回写 plan
  - `ovt.exe mix-audio --plan <edit.json> --output <mixed.wav> --preview`：通过，成功返回 `executionPreview` 与预期产物路径
  - `ovt.exe mix-audio --plan <edit.json> --output <mixed.wav> --overwrite`：通过，成功写出 `mixed.wav`
  - `ovt.exe scaffold-template-batch --manifest <batch.json>`：通过，成功写出 `summary.json` 与 `results/<id>.json`
  - `ovt.exe render-batch --manifest <batch.json> --preview`：通过，成功写出 `summary.json` 与 `results/<id>.json`

## 当前已确认边界（2026-04-26）

- `scaffold-template` 与裸 `init-plan` 的 owner 是“生成可编辑草稿 / 工作目录”，不是“默认直接可 render 的成片计划”。
- 对 `shorts-captioned` 这类模板，若没有 manual clip 编辑、`transcript` seed 或 `beats` seed，当前生成结果可以是 `isValid = true` 的空 clip 草稿。
- 因此 `v1.0` 的模板主闭环基线，应该写成：
  - `templates -> init-plan / scaffold-template -> validate-plan -> manual or seeded clips -> render`
  而不是默认宣称“任意裸 `init-plan` 一步到位即可 render”。
- 这不是当前仓库的实现故障，而是必须写进 release 边界的真实产品语义。
- sidecar 字幕在当前 `v1` render 路径中的实际语义也已通过发布物 smoke 确认：
  - `attach-plan-material` / `replace-plan-material` 可以稳定维护 `subtitles.mode = sidecar`
  - `render` 不把 sidecar 字幕烧录进视频，但会把字幕复制为输出同名 `.srt`

## 当前待补验证（2026-04-26）

- 站在发布物视角的主工作流 smoke 已有实测结果，但还没有整理成适合 release gate 复核的一页式证据包。
- `transcribe` / `separate-audio` 的 optional dependency 真实 smoke 本轮未执行，当前只能维持“文档已声明但不作为主阻塞项”的判断。
- `narrated`、`effects`、`export` 虽有既有 phase-check / acceptance 材料，但还没有在这张卡内汇总成 release 视角的已知限制列表。
- 当前仍未真正创建 `v1.0.0` tag / release；因此本卡现在能证明的是“当前发布纪律与能力边界已接近放行”，还不能替代最终发版动作本身。

## 当前初步结论（2026-04-26）

```text
当前是否已具备 v1.0 边界定义基础：是
当前是否已具备 v1.0 正式放行证据：否
当前主要缺口：发布视角的集中预检测证据包仍未补齐
当前更合理的动作：继续按本卡补主工作流 smoke、release binary 启动验证与缺陷归类，再决定是否进入 1.0.0
```

## 当前执行表基线（2026-04-26）

| 类别 | 条目 | 执行命令 / 入口 | 当前结果 | 缺陷级别 | 备注 |
| --- | --- | --- | --- | --- | --- |
| 资产 | release 资产完整性 | `src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj`、`.github/workflows/release.yml`、本地 tag `v0.1.0`、GitHub Release `v0.1.0` 页面 / API、`docs/plans/2026-04-25-e2-a2-phase-check.md`、本轮本地 `win-x64` publish smoke | `已确认（基于当前 v0.1.0 release 基线）` | 非阻塞 | 版本号、程序集名、tag、release workflow 与 GitHub Release 当前一致；已确认上传资产为 `ovt-linux-x64.tar.gz`、`ovt-osx-x64.tar.gz`、`ovt-win-x64.exe`、`ovt-win-x64.zip` |
| 契约 | CLI snapshot / JSON contract | `dotnet test .\OpenVideoToolbox.sln` | `已确认` | 非阻塞 | 当前全量测试通过：`388 passed`；可作为顶层 envelope、`--json-out`、失败路径、exit code 回归的第一层证据 |
| 主工作流 | `probe -> plan -> run` | 本地 release-style `ovt.exe` + 1 秒最小 `input.mp4` | `已确认` | 非阻塞 | 本轮发布物 smoke 已通过：`probe`、`plan`、`run` 均返回成功，且 `run` 成功写出 `smoke-output.mp4` |
| 主工作流 | `templates -> init-plan -> validate-plan -> render` | 本地 release-style `ovt.exe` + transcript-seeded smoke | `部分已确认` | 待定 | 裸 `init-plan` / `validate-plan` 已通过，但默认可生成空 clip 草稿；`seed-from-transcript` 路径下 `init-plan -> render --preview -> render` 已真实跑通 |
| 主工作流 | `scaffold-template -> inspect/attach/replace/bind -> render` | 本地 release-style `ovt.exe` + seeded plan maintenance smoke | `已确认（最小范围）` | 非阻塞 | 本轮发布物 smoke 已通过：`inspect-plan`、`attach-plan-material`、`replace-plan-material`、`bind-voice-track`、`render` 均成功；当前确认的是 `transcript-seeded + sidecar subtitles + voice bind` 这条最小维护链 |
| 主工作流 | `scaffold-template-batch -> render-batch` | 本地 release-style `ovt.exe` + batch smoke | `已确认（最小范围）` | 非阻塞 | 本轮发布物 smoke 已通过：`scaffold-template-batch` 成功写出 `summary.json` / `results/<id>.json`，`render-batch --preview` 也已成功写出 batch summary / result files |
| 主工作流 | `mix-audio --preview` / `mix-audio` | 本地 release-style `ovt.exe` + 1 秒最小音视频样例 + `bind-voice-track` 后 plan | `已确认` | 非阻塞 | 本轮发布物 smoke 已通过：`mix-audio --preview` 成功返回 `executionPreview` 与产物路径，`mix-audio --overwrite` 成功写出 `mixed.wav` |
| 主工作流 | `doctor` | `dotnet run --project .\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- doctor` + 近干净环境 fresh `ovt.exe doctor --json-out` | `已确认` | 非阻塞 | 当前本机与近干净环境样本均返回 `isHealthy = true`、`missingRequiredCount = 0`、`missingOptionalCount = 3`；required / optional 分层清晰，且 stdout 与 `--json-out` 一致 |
| 非主阻塞 | `transcribe` / `separate-audio` | `doctor` 输出 + 可选 real smoke | `当前机器未验证真实能力` | 非阻塞 | 当前机器缺 `whisper-cli` / `demucs` / `whisper-model`；只可确认错误边界清晰，不可确认默认环境可执行 |
| 非主阻塞 | `narrated` / `effects` / `export` | `docs/plans/v2/2026-04-25-v2-p6-acceptance-checklist.md`、`docs/plans/v2/2026-04-24-v2-p5-acceptance-checklist.md`、`docs/plans/v2/2026-04-26-v2-motion-effects-phase-check.md`、`docs/plans/v2/2026-04-26-v2-motion-effects-acceptance-checklist.md` | `已有阶段验收材料，未纳入 v1.0 主阻塞` | 非阻塞 | 当前更合理的口径是“受限附带能力”；后续只需整理成 release note 风险说明，不必抬成 `v1.0` 放行 blocker |

## 当前主工作流证据表（2026-04-26）

| 工作流 | 证据入口 | 当前结果 | 已确认语义 | 仍需补充 |
| --- | --- | --- | --- | --- |
| `doctor` | `dotnet run --project .\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- doctor` | 通过 | `required` / `optional` 依赖分层清晰；当前样本为 `missingRequiredCount = 0`、`missingOptionalCount = 3` | 更干净环境再跑一轮启动 smoke |
| `near-clean startup` | fresh `dotnet publish` 后的本地 `win-x64` single-file `ovt.exe`，从独立临时目录执行 `--help` 与 `doctor --json-out` | 通过 | 发布物不依赖仓库工作目录；未设置 `OVT_*` 环境变量时仍可正常启动；stdout 与 `--json-out` 一致 | 如需更强证据，可后续换到真正全新机器或沙盒再跑一次 |
| `probe -> plan -> run` | 新鲜 `dotnet publish` 后的本地 `win-x64` single-file `ovt.exe` + 1 秒最小 `input.mp4` | 通过 | 发布物视角下可完成最小传统转码链，`run` 成功写出 `.mp4` | 后续如要升 `1.0.0`，建议再补一次近干净环境复核 |
| `templates -> init-plan -> validate-plan -> render` | 同一份发布物 + `shorts-captioned` + transcript-seeded plan | 通过（带边界说明） | 裸 `init-plan` 可生成合法空 clip 草稿；`seed-from-transcript` 路径可真实 render | 需要在 release note 明写“valid draft != directly renderable output” |
| `scaffold-template -> inspect -> attach/replace -> render` | 同一份发布物 + seeded plan + sidecar subtitles | 通过 | `inspect-plan`、`attach-plan-material`、`replace-plan-material`、`render` 最小维护链成立；sidecar 字幕会作为 `.srt` 跟随成片输出 | 如需更强证据，可再补 beats / transcript attach 变体，但不应作为 `v1.0` blocker |
| `bind-voice-track` | 同一份发布物 + 最小 `edit.json` + `dub.wav` | 通过 | 默认成功建立 `voice-main` / `voice` 轨，并按 `relative` 路径回写 plan | 当前只验证了单轨最小 happy path，未单独覆盖 batch 变体 |
| `mix-audio --preview` / `mix-audio` | 同一份发布物 + 上述 voice-bound plan | 通过 | preview 返回稳定 `executionPreview` 与预期产物路径；执行成功写出 `mixed.wav` | 可选再补失败路径的发布物 smoke，但主失败契约已有自动化测试兜底 |
| `scaffold-template-batch -> render-batch` | 同一份发布物 + batch manifest smoke | 通过 | `summary.json` / `results/<id>.json` 约定在发布物视角下成立 | 如要形成 release 包，可把样例 manifest 与输出目录一并归档 |

## 当前 release gate 汇总（2026-04-26）

| Gate | 判定问题 | 当前状态 | 依据 | 结论 |
| --- | --- | --- | --- | --- |
| G1 | 发布资产定义是否清楚 | `通过（基于当前 v0.1.0 release 基线）` | 版本号、程序集名、tag、release workflow 已一致；GitHub Release latest `v0.1.0` 已在 **2026-04-26** 直检，发布时间为 `2026-04-24T02:50:22Z`，上传资产为 `4` 个 | 当前资产定义已足够支撑 `v1.0` 放行准备 |
| G2 | CLI 契约是否稳定 | `通过` | `dotnet test .\OpenVideoToolbox.sln` 通过，当前 `388 passed`；contract snapshot 与失败路径回归仍在 | 当前可视为 `v1.0` 放行基础已具备 |
| G3 | 主工作流是否具备发布物视角证据 | `通过（带边界说明）` | `doctor`、`probe -> plan -> run`、模板 seeded render、plan maintenance、`bind-voice-track`、`mix-audio`、batch smoke 已通过 | 当前主能力面接近可放行 |
| G4 | 非主阻塞能力是否已被正确降级 | `部分通过` | `narrated`、`effects`、`export`、optional dependency commands 已有文档和验收材料，但 release note 口径尚未集中 | 需要补一份限制说明，不必阻塞主线 |
| G5 | 是否已具备 `1.0.0` 最终放行证据 | `部分通过` | 近干净环境启动样本、GitHub Release 直检、已知限制基线与 release note 草案现已具备；但尚未实际创建 `v1.0.0` tag / release，也未完成最终 go / no-go 签发 | 当前更接近“可准备升版”，但仍不应跳过最终发版动作与结论签发 |

## 当前 release note 已知限制基线（2026-04-26）

如果后续要起草 `v1.0` release note，至少应把下面这些限制作为默认声明，而不是留给用户自行猜测：

1. `v1.0` 的正式支持面是 `CLI + Core`，不包含 `Desktop` 正式产品能力。
2. 模板链路中的“有效 plan”不等于“可直接 render 的成片 plan”；裸 `init-plan` / `scaffold-template` 允许生成 `clips = []` 的合法草稿，通常需要 manual clip 编辑、`transcript` seed 或 `beats` seed 后再进入 `render`。
3. `subtitles.mode = sidecar` 的当前语义是“跟随成片输出同名 `.srt`”，不是烧录进视频。
4. `transcribe` 与 `separate-audio` 依赖外部可选工具；默认环境未具备这些依赖时，不应把对应不可用解读为主工作流故障。
5. `init-narrated-plan`、`effects`、`export --format edl`、`schemaVersion = 2` 相关能力可以随发布物一起存在，但当前应按“受限附带能力 / 孵化能力”说明，而不是 `v1.0` 成立的核心承诺。
6. 当前简易 motion effects 只覆盖 `move / orbit / zoompan` 的受限 executor path，不是角色动画器、`Live2D`、骨骼系统或关键帧工作台。

## 当前执行注意事项（2026-04-26）

- 若使用本地 `publish/ovt.exe` 做 release-style smoke，必须先重新执行一次 `dotnet publish`，不能直接把历史 `publish` 目录中的旧二进制当成当前代码面的发布证据。
- 当前已实际踩到一次这个问题：旧的本地 `publish/ovt.exe` 不包含最新命令面；重新 publish 后 smoke 才与当前仓库命令面一致。
- 因此，本地发布物视角验证的标准动作应写成：
  1. 先 `dotnet publish`
  2. 再对 fresh binary 跑 smoke
  3. 最后记录命令面与产物结果
  否则很容易把“旧二进制过期”误判成“当前仓库实现回退”

## 当前基线读取方式

如果后续要基于这张卡继续补证据，建议按下面顺序读取当前基线，而不是重新发明一套检查口径：

1. 先看这张卡的 `当前执行表基线`
2. 再看 `docs/plans/2026-04-25-e2-a2-phase-check.md`
3. 再看 `docs/plans/v2/2026-04-24-v2-p5-acceptance-checklist.md`
4. 再看 `docs/plans/v2/2026-04-25-v2-p6-acceptance-checklist.md`
5. 再看 `docs/plans/v2/2026-04-26-v2-motion-effects-phase-check.md`

## 建议发布结论模板

```text
目标版本：
执行日期：
发布资产是否就绪：
CLI 契约是否稳定：
主工作流是否通过：
非主阻塞能力是否有已知缺陷：
阻塞缺陷列表：
可带说明发布的缺陷列表：
最终结论：可发布 v1.0 / 暂不发布 v1.0
后续动作：
```

## 当前建议放行结论草案（2026-04-26）

基于本卡当前已收集证据，如果今天就要给出一次正式重判，建议结论写成：

```text
目标版本：v1.0.0
执行日期：2026-04-26
发布资产是否就绪：部分就绪
CLI 契约是否稳定：是
主工作流是否通过：是（带边界说明）
非主阻塞能力是否有已知缺陷：有，但已可归类为受限附带能力或 optional dependency 能力
阻塞缺陷列表：
- 还未真正创建 `v1.0.0` tag / release
- 还未完成最终 go / no-go 签发
- 当前 release note 仍是草案，不是最终对外成稿
可带说明发布的缺陷列表：
- `transcribe` / `separate-audio` 依赖可选外部工具
- `init-narrated-plan` / `effects` / `export --format edl` / `schemaVersion = 2` 仍应标记为受限附带能力
- `subtitles.mode = sidecar` 当前只复制 `.srt`，不烧录进视频
最终结论：暂不发布 v1.0
后续动作：
- 先决定是否真的进入 `v1.0.0`
- 若进入，则创建 tag / release 并冻结最终 release note
- 最后按本卡模板补一版正式 go / no-go 结论
```

这份草案的重点不是否定当前能力，而是明确：当前主要缺的已经不是“实现”，而是最后一轮发布纪律。

## 建议最小放行证据包（2026-04-26）

如果后续真的要做 `1.0.0` 放行评审，建议最终只保留下面这一组最小证据包，不再无限追加材料：

1. 资产证据
   - `src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj` 当前版本与程序集名
   - `.github/workflows/release.yml` 当前目标平台与产物规则
   - GitHub Release 页面截图或人工复核记录
   - GitHub API 返回的最新 release 资产清单
2. 契约证据
   - `dotnet test .\OpenVideoToolbox.sln`
   - 当前通过数
   - 如有必要，补一条 contract snapshot 说明
3. 发布物 smoke 证据
   - fresh `dotnet publish`
   - 近干净环境 `--help` / `doctor --json-out`
   - `doctor`
   - `probe -> plan -> run`
   - `templates -> init-plan / validate-plan / seeded render`
   - `inspect / attach / replace / bind / render`
   - `mix-audio --preview` / `mix-audio`
   - `scaffold-template-batch -> render-batch`
4. 边界证据
   - “valid draft != directly renderable output”
   - `sidecar subtitles` 当前语义
   - optional dependency commands 的非阻塞口径
   - `narrated / effects / export / schemaVersion = 2` 的受限附带能力口径
5. 发布说明证据
   - release note 成稿
   - 已知限制列表
   - go / no-go 最终结论

只要这五类材料齐，就足够支持一次 `v1.0` 重判；再往上继续堆 phase-check、实现细节和历史讨论，信噪比会开始下降。

## 建议 release note 草案（2026-04-26）

下面这版不是最终 release note，只是当前更合理的初稿基线，供后续真正发版时压缩和改写：

```text
Open Video Toolbox v1.0.0

本版本将 Open Video Toolbox 定义为一个面向个人工作者、脚本和外部 AI 代理的稳定 CLI + Core 视频处理工具箱。

v1.0 正式承诺：
- 稳定的 CLI 结构化输出契约
- `doctor` 依赖预检
- 传统媒体处理链路：`probe` / `plan` / `run`
- 模板编辑主闭环：`templates` / `init-plan` / `scaffold-template` / `validate-plan` / `render`
- plan 维护闭环：`inspect-plan` / `attach-plan-material` / `replace-plan-material` / `bind-voice-track`
- 最小 batch 闭环：`scaffold-template-batch` / `render-batch`
- `mix-audio --preview` / `mix-audio`

已知限制：
- `v1.0` 不包含 Desktop 正式产品能力
- 裸 `init-plan` / `scaffold-template` 可能生成合法草稿，但不保证可直接 render；通常需要手工编辑 clips 或使用 transcript / beats seed
- `subtitles.mode = sidecar` 当前语义为输出同名 `.srt`，不是烧录字幕
- `transcribe` / `separate-audio` 依赖可选外部工具，不作为默认环境主工作流承诺
- `init-narrated-plan`、`effects`、`export --format edl`、`schemaVersion = 2` 当前仍属于受限附带能力
- 当前简易 motion effects 只覆盖 `move / orbit / zoompan` 的受限路径，不属于 `Live2D`、角色动画或关键帧系统

升级与兼容：
- `v1.0` 的核心承诺是现有 CLI contract 与主工作流稳定，不是扩成通用 NLE
- 如后续变更已发布命令的 JSON 契约、`--json-out` 语义或 exit code 语义，应按 major breaking change 处理
```

## 最后放行前的建议顺序（2026-04-26）

为避免最后阶段继续失焦，建议真正进入 `1.0.0` 前只按这个顺序执行：

1. fresh `dotnet publish`
2. 近干净环境启动 smoke
3. GitHub Release 页面与产物直检
4. 决定是否真的创建 `v1.0.0` tag / release
5. 把本卡中的“建议 release note 草案”压成最终对外说明
6. 用“建议发布结论模板”填一版最终 go / no-go 结论

如果这六步中有任一步还没做完，就不建议仓促把版本号抬到 `1.0.0`。

## 建议最终放行执行稿（2026-04-26）

下面这版执行稿的目标不是“解释为什么”，而是让执行人可以直接照着跑最后一轮 `v1.0` 放行检查。

### Step 1：冻结待发布代码面

执行前确认：

1. 待发布分支没有未解释的功能性增量。
2. 当前文档中的 `v1.0` 边界、已知限制、release note 草案已经是最新口径。
3. 不再把 `Desktop`、`narrated`、`effects`、`export` 或 optional dependency 链路临时抬成主阻塞项。

通过标准：

- 本卡与当前代码面一致
- 当前待发布范围仍是 `CLI + Core`

### Step 2：运行基础回归

建议命令：

```powershell
dotnet build .\OpenVideoToolbox.sln
dotnet test .\OpenVideoToolbox.sln
dotnet run --project .\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- doctor
```

通过标准：

- build 通过
- test 通过
- `doctor` 返回 `isHealthy = true`
- required dependency 缺失数为 `0`

### Step 3：生成 fresh release-style binary

建议命令：

```powershell
dotnet publish .\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

通过标准：

- fresh `publish\ovt.exe` 成功生成
- 不复用历史 `publish` 目录里的旧二进制做证据

### Step 4：跑近干净环境启动 smoke

最小目标：

- 从独立临时目录启动 `ovt.exe`
- 不依赖仓库工作目录
- 不依赖本轮手工设置的 `OVT_*` 环境变量

最小检查项：

```powershell
ovt.exe --help
ovt.exe doctor --json-out doctor.json
```

通过标准：

- `--help` 通过
- `doctor` 通过
- stdout 与 `doctor.json` 一致
- `missingRequiredCount = 0`

### Step 5：跑发布物主工作流 smoke

建议至少覆盖：

1. `probe -> plan -> run`
2. `templates -> init-plan -> validate-plan -> seeded render`
3. `inspect -> attach -> replace -> bind -> render`
4. `mix-audio --preview` / `mix-audio`
5. `scaffold-template-batch -> render-batch`

通过标准：

- 每条主工作流至少有一个最小 happy path 样本跑通
- 产物路径、summary 文件与结果文件符合当前契约
- 不把裸 `init-plan` 的空 clip 草稿误判为实现故障

### Step 6：直检 GitHub Release 资产

建议检查项：

1. latest tag 是否正确
2. release 是否为 `draft = false`
3. release 是否为 `prerelease = false`
4. 目标资产是否齐全
5. 资产名是否与文档口径一致

当前目标资产基线：

- `ovt-linux-x64.tar.gz`
- `ovt-osx-x64.tar.gz`
- `ovt-win-x64.exe`
- `ovt-win-x64.zip`

通过标准：

- release 页面与 API 返回结果一致
- 资产列表与 release workflow 当前目标产物一致

### Step 7：冻结最终 release note

最终稿至少必须包含：

1. `v1.0` 正式承诺能力
2. 已知限制
3. optional dependency 说明
4. 对后续 breaking change 的版本策略说明

通过标准：

- 不把附带能力写成主承诺
- 不遗漏 `valid draft != directly renderable output`
- 不遗漏 `sidecar subtitles` 当前语义

### Step 8：填写最终 go / no-go 结论

执行人最后只需要回答：

1. 当前 release 资产是否已就绪
2. CLI 契约是否稳定
3. 主工作流是否已通过
4. 剩余问题是否都属于可带说明发布项
5. 是否真的创建 `v1.0.0`

通过标准：

- 结论必须是明确的 `go` 或 `no-go`
- 不能再用“基本可以”“差不多”“先发再说”这类模糊表述

## 建议最终签发表（2026-04-26）

真正进入最后签发时，建议只保留下面这一页，不再把实现细节混进来：

```text
版本：v1.0.0
签发日期：
执行人：

1. 发布资产
- Latest tag：
- Release 页面已复核：是 / 否
- 目标资产齐全：是 / 否

2. 契约稳定性
- build：通过 / 未通过
- test：通过 / 未通过
- doctor：通过 / 未通过
- stdout 与 --json-out 一致：是 / 否

3. 主工作流
- probe -> plan -> run：通过 / 未通过
- templates -> init-plan -> validate-plan -> seeded render：通过 / 未通过
- inspect -> attach -> replace -> bind -> render：通过 / 未通过
- mix-audio --preview / mix-audio：通过 / 未通过
- scaffold-template-batch -> render-batch：通过 / 未通过

4. 已知限制
- 已写入 release note：是 / 否

5. 最终结论
- GO / NO-GO
- 阻塞项：
- 可带说明发布项：
```

## 当前更合理的下一步（2026-04-26）

如果本轮还继续推进，但仍不实际创建 `v1.0.0`，那最合理的动作已经只剩两个：

1. 把当前文档轮整理提交，作为 `v1.0` 评审基线。
2. 等待明确指令决定是否真的执行 `v1.0.0` 发版动作。

继续在这张卡里补更多细节，收益已经开始下降。

## 与当前路线图的关系

- 这张卡属于发布边界整理，不等于启动 `D1`
- 这张卡也不等于重开新的 `V2` 能力开发
- 它更接近 `E2` 的 release discipline 补强卡：先定义 `v1.0`，再决定什么时候真的升到 `1.0.0`

## 完成标准

只有同时满足以下条件，才算这张卡完成：

1. `v1.0` 的正式承诺能力与非承诺能力已分开。
2. 每条主工作流都已有明确验证方式。
3. 阻塞缺陷与非阻塞缺陷的分级规则已写清。
4. 后续执行人可以直接照表预检测，而不是重新发明口径。
