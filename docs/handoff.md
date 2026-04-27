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
d22d231 Fix preview-dir review issues
2cb2722 Align PKG1 directory offset validation with WC
ddd9ddc Add PKG1 version offset preview
5d32743 Clarify no-op PKG1 string key naming
3601367 Decode PKG1 directory entry names
e65c73a Add PKG1 directory preview
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
  - PKG1 top-level directory preview,
  - PKG1 directory-name decoding with no-op/KMS/GMS key modes,
  - PKG1 version hash and entry offset calculation,
  - PKG1 `0x02` string-reference name decoding,
  - PKG1 recursive directory-table pre-read and preview enumeration,
  - minimal IMG payload preview for top-level object type.

## Important Decisions

- Use .NET 10.
- Use Avalonia for the initial desktop UI shell.
- Build headless core and CLI before feature-heavy UI.
- Treat WC as a reference implementation, not a compatibility target.
- Preserve WC's format knowledge through tests before refactoring.

## Current State

Milestone 1 is implemented. Milestone 2 is in progress with real WZ package
header detection and PKG1 directory preview working against the local
MapleStoryNA client. `preview-dir` can decode names, detect PKG1 version/hash,
calculate entry offsets, and enumerate recursively nested directory tables.
`preview-img` can select an image by name/path/index and read its top-level IMG
object type.

## Suggested Next Prompt

Use this in a new Codex project conversation:

```text
We are continuing the WCX modernization project in this repository. Please read
AGENTS.md, docs/README.md, docs/handoff.md, docs/roadmap.md, and
docs/development-guidelines.md first. Then continue Milestone 2 by migrating
minimal IMG property/value parsing below the top-level object type. Add
fixture-backed tests, run build/test, update docs/logs, and commit the work on
the current branch.
```

## Caution

`origin` still points to `https://github.com/Kagamia/WzComparerX.git`. Do not
push to it unless the user explicitly asks and remote ownership is clarified.
