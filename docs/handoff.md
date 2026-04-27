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
    text/JSON output.

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

Milestone 1 is implemented. Milestone 2 is closing with real WZ package
header detection and PKG1 directory inspection working against the local
MapleStoryNA client. `inspect` now projects synthetic fixtures, WZ directories,
and WZ IMG payloads into a generic inspection tree so future UI/export/search
work does not depend directly on parser DTOs. `inspect --debug` exposes the
low-level fields that were useful during parser migration, including PKG1 node
types, sizes, checksums, hash offsets, calculated offsets, selected string key,
WZ/hash version, selected IMG entry metadata, object type, object value
metadata, property type/kind metadata, and Canvas/RawData/Video/Sound payload
offsets and lengths. `--key auto` selects among no-op/KMS/GMS directory string
decoding. `--depth 0` prints only the top-level IMG object type, while deeper
values expand bounded property/object metadata.

The old temporary parser/CLI terminology has been retired from active design
and command documentation. Historical logs may still mention migration-era
steps, but the supported surface is now `inspect` and `inspect --debug`.

Canvas pixel decoding and RawData/Video/Sound payload decoding are not
implemented yet. Lua image entries report script length and a short UTF-8
snippet, but full script export is not implemented yet. Text-format IMG streams
starting with `#Property` or `Root <Property>` inspect as bounded `Property`
trees.

## Suggested Next Prompt

Use this in a new Codex project conversation:

```text
We are continuing the WCX modernization project in this repository. Please read
AGENTS.md, docs/README.md, docs/handoff.md, docs/roadmap.md, and
docs/development-guidelines.md first. Continue Milestone 3 by starting the
export abstraction or by expanding diagnostics into a stable warning/error
model shared by CLI and future UI. Add fixture-backed tests, run build/test,
update docs/logs, and commit the work on the current branch.
```

## Caution

`origin` still points to `https://github.com/Kagamia/WzComparerX.git`. Do not
push to it unless the user explicitly asks and remote ownership is clarified.
