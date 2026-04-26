# Core Plan Snapshots

This directory stores golden files for machine-independent `edit.json` schema regression checks owned by `OpenVideoToolbox.Core.Tests`.

## Purpose

Use these snapshots to detect unintended drift in stable plan serialization shapes that:

- are owned by `OpenVideoToolbox.Core/Editing`
- are machine-independent
- are expected to stay backward-compatible during the current `schemaVersion = 1` observation window
- can be reproduced reliably in CI

These files are not a dumping ground for every plan variant or every generated artifact.

## Current Coverage

The current snapshot set intentionally covers only the canonical `schemaVersion = 1` plan sample used by `EditPlanSerializationTests`.

This sample exists to lock the externally visible serialization shape of the stable v1 plan model, not to exhaustively enumerate all legal plan combinations.

## When To Add A New Snapshot

Add a new Core plan snapshot only when all of the following are true:

1. The serialized shape is part of the long-lived plan contract.
2. The sample is deterministic and machine-independent.
3. Structural assertions alone would miss meaningful contract drift.
4. The additional sample represents a distinct canonical shape, not just another arbitrary fixture.

Good candidates:

- a canonical minimal plan shape with different contract significance
- a second canonical shape that exercises a separate long-lived serialization boundary

## When Not To Add A New Snapshot

Do not add snapshots for:

- environment-specific paths
- temporary execution artifacts
- plan variants that differ only in content, not contract shape
- cases already adequately protected by round-trip or targeted assertions

## Update Rules

Do not update a golden file just because a test failed.

A Core snapshot should change only when:

- a backward-compatible field is intentionally added and downstream impact is understood
- an incorrect serialized shape is intentionally corrected
- a breaking change has been explicitly accepted

If a snapshot changes, the change should explain:

1. why the serialized shape changed
2. whether the change is backward-compatible
3. whether `docs/roadmap.md`, `MODULE.md`, or other compatibility docs also need updates

## Preferred Test Strategy

Prefer the narrowest stable guardrail that still protects the contract:

- full snapshot for canonical machine-independent plan shapes
- round-trip tests for model read/write safety
- targeted assertions for localized field behavior

Do not expand this directory faster than the project can meaningfully review snapshot churn.
