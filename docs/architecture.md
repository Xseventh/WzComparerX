# WzComparerX Architecture

This document captures the intended shape of WzComparerX before implementation
starts. The main goal is to preserve WzComparerR2's hard-earned MapleStory
format knowledge while avoiding a direct port of its WinForms-era coupling.

## Goals

- Run on modern .NET and support macOS during core development.
- Keep the WZ/MS/PKG parsing core independent from any UI framework.
- Make browsing, exporting, comparing, and scripting usable from both CLI and UI.
- Treat MapRender, Avatar, CharaSim, and Lua support as modules, not as main-form
  features.
- Make behavior testable against small fixture files before broad UI work begins.

## Non-Goals

- Reimplement every WzComparerR2 feature in the first version.
- Preserve WzComparerR2's plugin ABI.
- Require Windows-only UI or DirectX dependencies in the core library.
- Build a full MapRender replacement before the resource browser and comparer are
  stable.

## Proposed Projects

```text
src/
  WzComparerX.WzLib
  WzComparerX.Core
  WzComparerX.Domain
  WzComparerX.Rendering
  WzComparerX.Cli
  WzComparerX.App

tests/
  WzComparerX.WzLib.Tests
  WzComparerX.Core.Tests
```

## Layer Responsibilities

### WzComparerX.WzLib

Owns low-level MapleStory file reading:

- WZ/MS/PKG file structures.
- Encryption and version detection.
- Lazy image extraction.
- PNG/audio/video raw payload parsing where practical.
- UOL/link resolution primitives.

This layer must not reference Avalonia, WinForms, MonoGame, or app services.

### WzComparerX.Core

Owns application-independent workflows:

- Workspace/session model.
- Opening and closing resource files.
- Node tree projection.
- Search.
- Export operations.
- Compare operations.
- Progress, cancellation, and diagnostics abstractions.

This layer may depend on `WzComparerX.WzLib`, but not on UI.

### WzComparerX.Domain

Owns MapleStory semantic models:

- Gear, item, skill, mob, npc, familiar, damage skin.
- Map metadata.
- String linking.
- Tooltip data models.
- Avatar data models.

This layer should convert raw WZ nodes into explicit domain objects.

### WzComparerX.Rendering

Owns rendering-independent adapters and platform-specific rendering modules:

- Image decode adapters.
- Animation frame projection.
- Tooltip rendering.
- Avatar rendering.
- Map rendering.

The first implementation can be modest. The important boundary is that rendering
receives explicit models instead of reaching back into global WZ state.

### WzComparerX.Cli

Provides automation and testable workflows:

- `list`
- `inspect`
- `export`
- `dump-xml`
- `compare`

The CLI should be useful before the desktop app is feature-complete.

### WzComparerX.App

Desktop application shell:

- Workspace UI.
- Node tree.
- Preview panes.
- Search and compare results.
- Settings.
- Task/log panel.
- Plugin host, once the internal command model is stable.

Avalonia is the preferred candidate if cross-platform support remains a goal.

## Dependency Direction

```text
App -> Core -> WzLib
App -> Domain -> WzLib
App -> Rendering -> Domain/Core/WzLib
Cli -> Core -> WzLib
Tests -> Core/WzLib/Domain
```

The core rule: dependencies flow inward toward file parsing and domain logic.
UI and plugins are outer layers.

## Plugin Direction

Do not begin with dynamic plugins. First define internal extension points:

- Commands.
- Viewers.
- Exporters.
- Compare analyzers.
- Decoders.

Once these are stable, expose them through a plugin SDK.

## First Milestone

The first milestone is a headless, testable resource browser:

- Open one WZ-like fixture.
- Enumerate the node tree.
- Inspect node metadata.
- Export one supported data type.
- Run from CLI on macOS.

The desktop UI should start only after these workflows are stable.
