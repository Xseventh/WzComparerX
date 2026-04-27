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

## Migrated Behaviors

### WZ Package Header Detection

- WC source files referenced:
  - `WzComparerR2.WzLib/Wz_Header.cs`
  - `WzComparerR2.WzLib/Wz_File.cs`
- WC behavior preserved:
  - Recognize `PKG1` and `PKG2` signatures.
  - Read data size, header size, copyright, and directory start position.
  - Preserve WC's PKG1 encrypted-version-missing heuristic for values above
    `0xff` and the compressed-int `0x80` edge case.
  - Read PKG2 header hash fields.
- Fixture or sample used:
  - Minimal in-memory synthetic WZ header byte streams in
    `WzPackageHeaderReaderTests`.
- Test coverage added:
  - Valid PKG1 with encrypted version.
  - PKG1 with missing encrypted version.
  - Valid PKG2 hash fields.
  - Invalid signature handling.
- Known unsupported cases:
  - WZ version profile detection is not migrated yet.
  - Directory tree parsing is not migrated yet.
  - MS/MN and PKG2 string encryption detection are not migrated yet.
- UI dependency removed or isolated:
  - Implemented in `WzComparerX.WzLib` only; no UI dependency.

### PKG1 Directory Inspection And Names

- WC source files referenced:
  - `WzComparerR2.WzLib/Wz_File.cs`
  - `WzComparerR2.WzLib/Utilities/WzBinaryReader.cs`
  - `WzComparerR2.WzLib/Wz_Crypto.cs`
  - `WzComparerR2.WzLib/Compatibility/WzOffsetCalc.cs`
  - `WzComparerR2.WzLib/Compatibility/WzVersionVerifier.cs`
  - `WzComparerR2.WzLib/Utilities/MathHelper.cs`
- WC behavior preserved:
  - Read PKG1 compressed entry count.
  - Read top-level directory/image entry type, size, checksum, and raw hash
    offset fields.
  - Decode inline PKG1 ASCII/UTF-16 directory strings with no-op, KMS, and
    GMS key modes.
  - Decode PKG1 `0x02` string-reference entry names.
  - Calculate PKG1 hash versions and entry offsets.
  - Detect missing-encrypted-version PKG1 files as WZ version `777`.
  - Detect encrypted-version PKG1 files by validating calculated offsets.
  - Enumerate recursively nested PKG1 directory tables in WC's linear layout.
  - Read the top-level IMG object type name from calculated image offsets.
  - Inspect supported top-level non-`Property` IMG object values.
  - Read first-layer `Property` entries, including simple scalar values and
    nested object summaries.
  - Expand nested IMG `Property` objects to a caller-provided bounded depth.
  - Inspect nested `Shape2D#Vector2D`, `Shape2D#Convex2D`, `UOL`, Canvas
    metadata, RawData metadata, Canvas#Video metadata, and Sound_DX8 metadata
    object values.
  - Detect Canvas payload compression kind and expected uncompressed data
    length for WC texture formats.
  - Auto-detect inspection string key selection across no-op, KMS, and GMS modes
    using conservative directory-name scoring.
  - Inspect `.lua` IMG entries through WC's Lua-specific block stream path.
  - Inspect WC text-format IMG v1 (`#Property`) and v2 (`Root <Property>`)
    streams.
- Fixture or sample used:
  - Minimal in-memory synthetic WZ byte streams in
    `WzDirectoryInspectionReaderTests`.
  - Local-only MapleStoryNA smoke file `Data/Base/Base.wz`.
- Test coverage added:
  - PKG1 directory entry pre-read with encoded no-op strings.
  - PKG1 `0x02` string-reference names.
  - PKG1 recursive child directory table enumeration.
  - PKG1 hash-version and offset calculation.
  - IMG object type inline and referenced strings.
  - First-layer IMG `Property` scalar entries.
  - PKG1 encrypted-version detection against deterministic synthetic offsets.
  - Core inspection string key auto-detection against deterministic synthetic
    directory data.
  - Core text formatter output for decoded inspection entries.
  - Bounded nested IMG `Property` inspection.
  - IMG Vector, Convex2D, UOL, Canvas metadata, RawData metadata, and
    Canvas#Video metadata value inspection.
  - Canvas zlib/chunked payload metadata and uncompressed data size estimates.
  - IMG Sound_DX8 metadata value inspection.
  - Top-level IMG Canvas metadata and Vector value inspection.
  - Lua image block inspection and `--depth 0` behavior.
  - Text IMG v1/v2 scalar and nested property inspection.
- Known unsupported cases:
  - Full unbounded IMG property/value parsing is not implemented yet.
  - Text IMG v2 multiline string parsing is not implemented yet.
  - Canvas pixel decoding is not implemented yet.
  - RawData payload decoding is not implemented yet.
  - Canvas#Video payload decoding is not implemented yet.
  - Sound_DX8 audio payload decoding is not implemented yet.
  - Other nested IMG object payloads are still summarized by object type.
- UI dependency removed or isolated:
  - Implemented in `WzComparerX.WzLib` and Core CLI services only; no UI
    dependency.

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
