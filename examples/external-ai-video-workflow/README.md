# External AI Video Workflow Example

This directory is a workflow example, not a new CLI contract.

It shows how to adapt YouTube-clipper-style work into Open Video Toolbox without adding an in-repo AI provider or direct FFmpeg command assembly.

## Files

| File | Purpose |
| --- | --- |
| `signals/chapters.sample.json` | Example external-AI-authored chapter list matching `docs/schemas/chapters.schema.json` |
| `commands.example.ps1` | Copyable command sequence using current OVT commands |
| `render-batch.sample.json` | Example render batch manifest after task workdirs already exist |
| `tasks/chapter-02/copy.sample.md` | Example publishing copy artifact kept beside a chapter task |

## Workflow

1. Download or otherwise obtain a local `source/source.mp4` outside OVT.
2. Run `probe`, and optionally `transcribe` / `subtitle`.
3. Have an external AI or human write `signals/chapters.sample.json`.
4. For each selected chapter, create a task workdir with `scaffold-template`.
5. Attach externally produced subtitles or voice tracks with OVT CLI.
6. Run `validate-plan`, `render --preview`, then `render`.

See `docs/plans/2026-04-29-external-ai-video-workflow-design.md` for the complete design.
