# Migration From WzComparerR2

This document tracks how WzComparerX should reuse WzComparerR2 without becoming
a line-by-line rewrite of it.

## Source Project Names

- WC: WzComparerR2, the maintained existing implementation.
- WCX: WzComparerX, the new architecture described in this repository.

## Migration Principles

- Preserve format knowledge before preserving UI behavior.
- Prefer small, testable ports over large copied modules.
- Keep Windows-only dependencies out of the core.
- Add tests around migrated behavior as soon as a fixture exists.
- Treat old public surface area as reference material, not as a compatibility
  contract.

## High-Value WC Assets

### WzComparerR2.WzLib

Highest priority. It contains WZ/MS parsing, version detection, encryption,
image extraction, checksums, and compatibility logic.

Porting approach:

1. Copy a minimal subset into `WzComparerX.WzLib`.
2. Remove UI and framework-specific assumptions.
3. Keep behavior close to WC until tests prove safe refactors.
4. Add fixture-backed tests for each supported format case.

### Comparer

Worth migrating after basic browsing works. WC already contains compare logic
for virtual WZ nodes and report generation.

Porting approach:

1. Extract comparison model first.
2. Separate comparison computation from HTML/report rendering.
3. Add JSON output before adding UI diff views.

### CharaSim Domain Models

Valuable but broad. Item, gear, skill, familiar, damage skin, and set item logic
should move into explicit domain models.

Porting approach:

1. Start with read-only models.
2. Avoid moving custom WinForms tooltip renderers early.
3. Add tooltip data projection before visual rendering.

### Avatar

Important user-facing feature, but not part of the first milestone.

Porting approach:

1. Separate data assembly from drawing.
2. Define a frame/layer output model.
3. Add rendering after the data model stabilizes.

### MapRender

Large and high-risk. It should be treated as a separate module.

Porting approach:

1. Port map data loading separately from rendering.
2. Define a scene graph model independent of MonoGame.
3. Choose rendering backend later.

## Areas To Avoid Porting Directly

- `MainForm` orchestration and event wiring.
- Designer-generated WinForms UI.
- Global `PluginManager.FindWz` as the primary resource lookup mechanism.
- Direct DotNetBar dependencies.
- Direct SharpDX dependencies in shared logic.
- WindowsDX-specific assumptions in core or domain projects.

## Compatibility Checklist

For each migrated feature, record:

- WC source files referenced.
- WC behavior being preserved.
- Fixture or sample used.
- Test coverage added.
- Known unsupported cases.
- UI dependency removed or isolated.

## Suggested Migration Order

1. Repository and solution skeleton.
2. WZ node model and basic file open.
3. Lazy image extraction.
4. Node listing CLI.
5. XML/JSON dump.
6. Image export.
7. Search.
8. Compare model.
9. Compare report output.
10. Basic Avalonia browser.
11. Domain models for item/gear/skill.
12. Tooltip data projection.
13. Avatar data model.
14. Map data model.
15. Rendering modules.
