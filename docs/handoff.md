# WCX Handoff

This file lets a new Codex conversation resume the project without access to the
original chat.

## Repository

Path used in the previous Codex thread:

```text
/Users/seventh/Documents/Codex/2026-04-26/https-github-com-kagamia-wzcomparerr2-https/WzComparerX
```

Current development branch:

```text
codex/wcx-modernization
```

Recent commits:

```text
6eaf391 Add Canvas export value selector
dfa9cc5 Define M3 Canvas export boundary
fa5aeb9 Remove ambiguous string key alias
6b08714 Reject JSON flag for export
b042af9 Close out milestone 2
6383edf Remove retired diagnostic terminology remnants
5071c93 Sync M3 fixture documentation
a6206ab Add text and Lua export fixtures
099c0d4 Stabilize Canvas decode diagnostic
fb91636 Add Canvas inspect JSON golden
a42af46 Rename parser diagnostic models to inspection
d6fc84b Add inspect debug diagnostics
84a5f88 Add resource inspect abstraction
41e1ad0 Fix PKG1 string reference offset base
2cb2722 Align PKG1 directory offset validation with WC
5d32743 Clarify no-op PKG1 string key naming
3601367 Decode PKG1 directory entry names
6f18aef Add WZ header scan command
3285065 Add JSON output for CLI workflows
bbfe559 Add CLI WZ header command
11b974c Add WZ package header detection
79e5568 Add headless resource browser
53598d9 Add WCX development governance docs
a0858cb Bootstrap WCX modernization project
```

## What Has Been Done

- Cloned WC and WCX.
- Inspected WC's architecture, complexity, and maintenance state.
- Established that WCX should not be a direct WC UI port.
- Installed/verified local tooling:
  - .NET SDK `10.0.203`
  - ripgrep `15.1.0`
  - ffmpeg `8.1_1`
  - ImageMagick `7.1.2-21`
- Added `/usr/local/share/dotnet` to `~/.zshrc`.
- Created `.NET 10` WCX solution skeleton.
- Added Avalonia MVVM app shell.
- Added CLI, Core, Domain, Rendering, WzLib, and test projects.
- Added development docs, ADRs, roadmap, command reference, and bootstrap log.
- Verified build and tests after bootstrap.
- Implemented Milestone 1 headless resource browser:
  - synthetic raw node fixture and reader,
  - Core document/workspace/list services,
  - CLI `list` command,
  - deterministic tests.
- Started Milestone 2 real parser migration:
  - WZ `PKG1`/`PKG2` header detection,
  - CLI `header` and recursive `headers` commands,
  - JSON output for `list` and header workflows,
  - PKG1 top-level directory inspection,
  - PKG1 directory-name decoding with no-op/KMS/GMS key modes,
  - PKG1 version hash and entry offset calculation,
  - PKG1 `0x02` string-reference name decoding,
  - PKG1 recursive directory-table pre-read and inspection enumeration,
  - minimal IMG payload inspection for top-level object type,
  - bounded IMG `Property` inspection for first-layer scalar values and nested
    `Property` objects,
  - IMG Vector, Convex2D, UOL, Canvas metadata, RawData metadata, and
    Canvas#Video metadata value inspection,
  - Canvas payload compression metadata and uncompressed size estimates,
  - IMG Sound_DX8 metadata value inspection,
  - string-key auto-detection across no-op/KMS/GMS modes,
  - top-level IMG object value inspection for supported non-`Property` object
    types,
  - Lua image block inspection for `.lua` entries,
  - text-format IMG v1/v2 property inspection,
  - initial Core `inspect` model and CLI command,
  - `inspect --debug` with structured directory and IMG diagnostics,
  - CLI-level golden tests for representative `inspect` and `inspect --debug`
    text/JSON output,
  - initial Core export abstraction and CLI `export` command for metadata, WC
    text-format IMG streams, and Lua IMG scripts,
  - export `--out` file output, byte-oriented export documents, Lua multi-block
    concatenation, and structured export diagnostics for unsupported exports,
  - split Lua, text IMG, and binary IMG inspection readers out of
    `WzImageInspectionReader`,
  - stable diagnostic codes/sources for parser payload limitations and export
    conditions.

## Important Decisions

- Use .NET 10.
- Use Avalonia for the initial desktop UI shell.
- Build headless core and CLI before feature-heavy UI.
- Treat WC as a reference implementation, not a compatibility target.
- Preserve WC's format knowledge through tests before refactoring.
- `inspect` is the long-term resource observation surface.
- `inspect --debug` is the development diagnostic surface for low-level parser
  metadata.

## Current State

Milestones 1, 2, and 3 are complete. The current mainline is Milestone 4 Basic
Avalonia Browser. M4 should reuse Core inspection/export models and must not add
parser behavior directly in app view models. M2 delivered real WZ package header
detection, PKG1 directory inspection, recursive directory entries, string-key
handling, IMG object and property metadata, Lua/text IMG inspection, and payload
metadata for Canvas, RawData, Video, and Sound values.

`inspect` now projects synthetic fixtures, WZ directories, and WZ IMG payloads
into a generic inspection tree so future UI/export/search work does not depend
directly on parser DTOs. `inspect --debug` exposes the low-level fields that
were useful during parser migration, including PKG1 node types, sizes,
checksums, hash offsets, calculated offsets, selected string key, WZ/hash
version, selected IMG entry metadata, object type, object value metadata,
property type/kind metadata, and Canvas/RawData/Video/Sound payload offsets and
lengths. `--key auto` selects among no-op/KMS/GMS directory string decoding.
`--depth 0` prints only the top-level IMG object type, while deeper values
expand bounded property/object metadata.

The old temporary parser/CLI terminology has been retired from active design,
command documentation, file names, and active tests. The supported observation
surface is now `inspect` and `inspect --debug`.

Canvas pixel decoding has the first narrow direct-zlib raw-byte slice for
format `2` / `2562`; PNG export and broader Canvas format coverage remain
later work. RawData/Video/Sound payload decoding is not implemented yet. Lua image
entries report script length and a short UTF-8 snippet; `export --type lua`
writes the full decoded script for supported Lua IMG blocks. Text-format IMG
streams starting with `#Property` or `Root <Property>` inspect as bounded
`Property` trees and can be exported with `export --type text`. Text exports
write to stdout by default or exact bytes to `--out <path>`; Canvas export is
binary-only and requires `--out`. Diagnostics carry stable severities, codes,
sources, and resource paths through `ResourceInspectionDiagnostics`; CLI text
output prints codes in brackets when present and avoids volatile raw exception
text. The diagnostic rules are documented in `docs/diagnostics.md`.
`WzImageInspectionReader` is now the small image-entry dispatcher, and
`WzImageBinaryInspectionReader` is the binary IMG entry coordinator. Binary IMG
object/property parsing lives in `WzImageBinaryObjectInspectionReader`, with
bounded property traversal in `WzImageBinaryPropertyInspectionReader` and IMG
string decoding in `WzImageBinaryStringReader`. Text and Lua stream shapes are
in their own readers. Canvas/RawData/Video/Sound payload metadata lives in
`WzImagePayloadInspectionReader`, and shared IMG binary read primitives live in
`WzImageBinaryReaderPrimitives`; the next parser split should target additional
object-type families only when new behavior needs them.

Canvas decode/export follows the narrow plan in
`docs/canvas-decode-export-plan.md`: synthetic fixture first, direct zlib and a
single verified pixel format first, payload decoder separate from parser
metadata and Core export. The first raw-byte `export --type canvas --out` slice
exists for direct zlib Canvas payloads with format `2` / `2562`. PNG export is
not required for M3; it remains later user-facing image export work. Canvas
export now uses `--value <property-path>` for IMG-internal Canvas selection; the
selector design is in `docs/canvas-export-selector-plan.md`.
Committed synthetic PKG1 hex fixtures now cover Canvas, WC text-format IMG, and
Lua IMG export paths, with expected text/JSON/stdout/stderr golden outputs
under `fixtures/expected`.

Recent CLI boundary cleanup:

- `export --json` now returns a usage error because export writes resource
  content directly; metadata export is already JSON.
- The current string-key CLI surface is `auto|none|noop|kms|gms`. The no-op mode
  should not be presented as a service-region key.

M3 closeout:

- `inspect`, `inspect --debug`, and `export` are the headless automation
  surfaces.
- Export covers metadata JSON, WC text-format IMG, Lua IMG, and raw direct-zlib
  Canvas bytes.
- Diagnostics have stable severities, sources, codes, CLI text formatting, and
  docs.
- XML dump, PNG export, broader Canvas decode, audio/video decode, and full PKG2
  directory parsing are later work.

M4 started:

- The Avalonia shell now has a path-based resource loader.
- The main window view model calls `ResourceInspectionService` and projects Core
  inspection nodes into a UI tree.
- The UI has an IMG selector field plus an `Inspect Image` action for selected
  image nodes. String key and depth inputs mirror the current inspect workflow.
- The path row has one `Browse` entry with native file and folder picker menu
  choices. The `Load` action automatically handles file paths and folder paths.
  Folder paths scan `.wz` headers into a folder inspection tree; selecting a
  package node enables `Open Package`. Opening a new file or package clears the
  previous IMG selector before loading.
- Double-clicking activatable resource nodes routes through the ViewModel:
  package nodes open their package, and image nodes run the same image
  inspection action as the `Inspect Image` button.
- The UI shows document metadata, selected-node metadata, and selected
  diagnostics.
- The UI has a basic activity log for load, inspection, and error events.
- `WzComparerX.App.Tests` now owns Avalonia UI/ViewModel tests and uses
  Avalonia Headless for window/control smoke coverage. Core.Tests no longer
  references the App project.
- Richer task/progress handling is still pending.

Local GMS smoke status:

- Base, Base_000, UI_000, Sound_000, and WZ2Lua directory-only smokes are
  recorded in the M2 closeout log.
- Lua IMG and WC text-format IMG behavior is locked by synthetic fixtures, but
  still needs direct real-sample smoke verification when a suitable local
  client entry is found.

## Suggested Next Prompt

Use this in a new Codex project conversation:

```text
We are continuing the WCX modernization project in this repository. Please read
AGENTS.md, docs/README.md, docs/handoff.md, docs/roadmap.md, and
docs/development-guidelines.md first. Continue Milestone 4: Basic Avalonia
Browser. Reuse Core inspection/export models, keep parsing out of view models,
add focused UI/view-model tests where practical, run build/test, update
docs/logs, and commit the work on the current branch.
```

## Caution

`origin` still points to `https://github.com/Kagamia/WzComparerX.git`. Do not
push to it unless the user explicitly asks and remote ownership is clarified.
