---
name: open-video-toolbox-workflow
description: Use this skill when working in the Open Video Toolbox repository or orchestrating its CLI for video workflows: dependency checks, template discovery, scaffolded edit.json workdirs, subtitles/transcripts/beats/silence signals, external-AI chapter workflows, material attach/replace, validation, preview, render, and batch execution. Also use when adapting YouTube-clipper-style workflows without adding in-repo AI providers or direct FFmpeg command assembly.
---

# Open Video Toolbox Workflow

Use OVT as a deterministic CLI video production kernel. Keep AI reasoning outside the product; use CLI commands and structured files as the contract.

## Boundaries

- Do not add AI provider SDKs, remote model calls, or translation APIs to the repo.
- Do not assemble `ffmpeg` / `ffprobe` commands in UI or ad hoc scripts when an OVT CLI/Core path exists.
- Do not introduce a second owner for subtitles, media probing, process execution, or `edit.json` semantics.
- For YouTube or webpage videos, download externally first. OVT consumes local files and structured sidecars.
- Preserve stdout / stderr / exit code / timeout context for external tools; prefer existing OVT commands over shell snippets.

## Standard Loop

1. Inspect the repo contract before changing behavior:
   - `README.md`
   - `docs/PROJECT_PROFILE.md`
   - `docs/ARCHITECTURE_GUARDRAILS.md`
   - relevant `MODULE.md`
2. Check dependencies:
   - `<ovt> doctor --json-out doctor.json`
3. Discover templates and examples:
   - `<ovt> templates --summary`
   - `<ovt> templates <template-id> --write-examples .template-guide`
4. Create or inspect a workdir:
   - `<ovt> scaffold-template input.mp4 --template <template-id> --dir .workspace --validate --check-files`
   - `<ovt> inspect-plan --plan .workspace/edit.json --check-files --json-out .workspace/inspect.json`
5. Attach or replace externally produced materials:
   - transcript: `attach-plan-material --transcript`
   - subtitles: `attach-plan-material --subtitles`
   - beats: `attach-plan-material --beats`
   - voice: `bind-voice-track`
6. Validate before rendering:
   - `<ovt> validate-plan --plan .workspace/edit.json --check-files`
   - `<ovt> render --plan .workspace/edit.json --preview --json-out .workspace/render-preview.json`
7. Execute only after preview is sane:
   - `<ovt> render --plan .workspace/edit.json --output final.mp4`

## Choose the Entry

| User intent | Preferred entry |
| --- | --- |
| Quick dependency check | `doctor` |
| Understand input media | `probe` |
| Traditional transcode | `plan` then `run` |
| Template-based video | `templates` then `init-plan` or `scaffold-template` |
| Batch workdirs | `scaffold-template-batch` |
| Batch rendering | `render-batch` |
| Add transcript/subtitles/beats | `attach-plan-material` or batch variant |
| Swap existing media | `replace-plan-material` or batch variant |
| Add external voice/TTS result | `bind-voice-track` or batch variant |
| Validate AI/manual edits | `validate-plan` and `inspect-plan` |
| Export rough NLE cut list | `export --format edl` |

## External AI Chapter Workflow

Use this for YouTube-clipper-style requests.

1. Ensure source media is local:
   - External download tools are allowed as a user-side pre-step.
   - Do not present `yt-dlp` as an OVT built-in command.
2. Get text:
   - Prefer existing subtitles if available.
   - Otherwise use `<ovt> transcribe input.mp4 --model <model> --output transcript.json`.
3. Ask the external AI to produce a chapter file with at least:
   - `id`
   - `title`
   - `start`
   - `end`
   - `summary`
   - optional `keywords`
4. For simple clips, call `cut` per selected chapter.
5. For publishable outputs, create a template workdir per chapter, attach subtitles/voice/BGM as files, validate, preview, then render.

Keep chapter analysis, translation, and publish copy as external artifacts such as `chapters.json`, translated `.srt`, and `copy.md`. OVT should validate or consume only the parts it owns.

Use these repository-local references when shaping files:

- `docs/schemas/chapters.schema.json`
- `examples/external-ai-video-workflow/signals/chapters.sample.json`
- `examples/external-ai-video-workflow/commands.example.ps1`

## Subtitle and Bilingual Workflow

1. Generate or obtain transcript/subtitle:
   - `<ovt> transcribe input.mp4 --model <model> --output transcript.json`
   - `<ovt> subtitle input.mp4 --transcript transcript.json --format srt --output subtitles.srt`
2. If an external AI translates subtitles, write the translated or bilingual file as a sidecar.
3. Attach it to the plan:
   - `<ovt> attach-plan-material --plan edit.json --subtitles --path subtitles.srt --check-files`
4. Validate and preview:
   - `<ovt> validate-plan --plan edit.json --check-files`
   - `<ovt> render --plan edit.json --preview --json-out render-preview.json`

Do not implement translation providers inside OVT.

## Batch Pattern

Use manifest-driven commands when there is more than one target. Keep results predictable:

- manifest-relative paths
- top-level `summary.json`
- per-item `results/<id>.json`
- exit code `0` for all success, `2` for item failure, `1` for manifest/load failure

Prefer existing batch commands before proposing a new one:

- `scaffold-template-batch`
- `render-batch`
- `replace-plan-material-batch`
- `attach-plan-material-batch`
- `bind-voice-track-batch`

## Final Checks

Before considering a workflow complete:

- `inspect-plan --check-files` or `validate-plan --check-files` has run when a plan exists.
- `render --preview` or `render-batch --preview` has been reviewed before execution.
- Any new external dependency or owner change is documented before implementation.
- Documentation distinguishes implemented CLI commands from external workflow guidance.
