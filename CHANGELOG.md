# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Strategy

- **Initial development (`0.0.1`):** The first runnable repository version; the API and CLI contract are experimental.
- **Pre-1.0 (`0.x.y`):** New commands and contract changes may still require migration notes before the stable release.
- **Major (`x.0.0`):** Breaking changes to the CLI output contract (`--json-out` envelope structure, field removal/rename) after the stable release. Always announced with migration notes.
- **Minor (`x.y.0`):** Backward-compatible commands, template categories, plugin capabilities, and other new features.
- **Patch (`x.y.z`):** Bug fixes, dependency updates, and documentation corrections without contract changes.

### Breaking Change Policy

A change is **breaking** if any of the following change for an existing command:

- Top-level envelope field names (`command`, `preview`, `payload`)
- Payload field names or types that were present in a released version
- Exit code semantics (e.g., 0/1/2 meaning changes)
- `--json-out` file content differs from stdout

Breaking changes **must**:
1. Bump the major version
2. Include migration notes in this changelog
3. Update contract snapshot golden files

### Contract Change Decision Table

| Change type | Breaking | Update golden files | Migration notes | Version impact |
|-------------|----------|---------------------|-----------------|----------------|
| Top-level envelope field removal / rename (`command`, `preview`, `payload`) | Yes | Yes | Required | Major |
| Existing payload field removal / rename / type change | Yes | Yes | Required | Major |
| Exit code semantic change | Yes | Yes if affected output expectations exist | Required | Major |
| `stdout` and `--json-out` content diverge | Yes | Yes after fixing behavior | Required if released behavior changes | Major |
| New optional payload field added without changing existing semantics | No | Yes | Usually not required | Minor |
| Field order change only | No | No unless snapshot intentionally stores ordered text | No | Patch |
| Machine-specific path / temp directory variance with unchanged semantic shape | No | Prefer test normalization, not golden file churn | No | Patch |

### Contract Snapshot Rules

- Golden files may be updated only when:
  - a backward-compatible field is intentionally added
  - an incorrect output is intentionally corrected and downstream impact has been reviewed
  - a breaking change has been explicitly accepted
- Updating golden files alone is not a sufficient fix when:
  - a field was removed or renamed without an explicit compatibility decision
  - the top-level envelope changed
  - `stdout` and `--json-out` no longer match
  - exit code semantics changed without migration notes
- Prefer machine-independent structural comparison over volatile full-text snapshots when output includes:
  - absolute paths
  - temp directories
  - other environment-specific values

## [Unreleased]

### Added
- Stable `CLI + Core` media toolbox surface for:
  - `doctor`
  - `probe` / `plan` / `run`
  - `cut` / `concat` / `extract-audio`
  - `audio-analyze` / `audio-gain` / `audio-normalize`
  - `templates` / `init-plan` / `scaffold-template` / `validate-plan` / `render`
  - `inspect-plan` / `replace-plan-material` / `attach-plan-material` / `bind-voice-track`
  - `scaffold-template-batch` / `render-batch`
  - `mix-audio --preview` / `mix-audio`
- Unified command envelope (`{ command, preview, payload }`) and `--json-out` support for the shipped CLI surface
- Structured failure envelope on error paths instead of usage text fallback
- Static template plugin discovery via `--plugin-dir`
- `doctor` dependency pre-check for required and optional external tools
- Contract snapshot tests and real-smoke coverage for release-critical workflows
- Cross-platform single-file release workflow for:
  - `win-x64`
  - `linux-x64`
  - `osx-x64`

### Notes
- The public `v0.1.0` release is the historical pre-1.0 baseline. The current project version is `1.0.0`, while the `v1.0.0` tag and GitHub Release remain pending; this `Unreleased` section is release-note preparation, not proof that `1.0.0` has already shipped.
- The intended formal support surface for the next major release is `CLI + Core`; `Desktop` is not part of that support surface.
- `init-narrated-plan`, `effects`, `export --format edl`, and ongoing `schemaVersion = 2` capabilities may ship alongside the product, but are currently treated as attached / limited capabilities rather than the core `v1.0` promise.
- `transcribe` and `separate-audio` depend on optional external tools and should not be interpreted as default-environment guarantees.
- A valid draft plan is not always directly renderable output; bare `init-plan` / `scaffold-template` may produce a valid draft with no clips until the plan is manually edited or seeded from transcript / beats.
- `subtitles.mode = sidecar` currently means copying a same-name `.srt` next to the rendered output, not burning subtitles into the video.
