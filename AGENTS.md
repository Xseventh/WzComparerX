# WCX Agent Instructions

This repository is a long-running Codex-led modernization project for
WzComparerX.

## First Things To Read

When starting or resuming work, read these files in order:

1. `docs/README.md`
2. `docs/handoff.md`
3. Latest file in `docs/logs/`
4. `docs/roadmap.md`
5. `docs/development-guidelines.md`

## Project Naming

- WC: WzComparerR2, the existing maintained project.
- WCX: WzComparerX, this modernization project.

## Current Branch

Primary local development branch:

```text
codex/wcx-modernization
```

Do not push to `origin` unless the user explicitly asks and the remote strategy
has been clarified. `origin` currently points to the original upstream
repository.

## Core Direction

WCX should preserve WC's MapleStory resource-format knowledge while avoiding a
direct port of WC's WinForms-era coupling.

Build order:

1. Headless core.
2. CLI workflows.
3. Tests and fixtures.
4. Basic Avalonia UI.
5. Real WC parser migration.
6. Higher-level features such as compare, CharaSim, Avatar, MapRender.

## Current Next Milestone

Milestone 1: Headless Resource Browser.

Immediate tasks:

- Define minimal raw node model in `WzComparerX.WzLib`.
- Define document/workspace model in `WzComparerX.Core`.
- Add synthetic fixture under `fixtures/synthetic/`.
- Implement CLI command:

```bash
dotnet run --project src/WzComparerX.Cli -- list fixtures/synthetic/basic-tree.json
```

- Add deterministic tests.

## Commands

Restore:

```bash
dotnet restore WzComparerX.slnx
```

Build:

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
```

Test:

```bash
dotnet test WzComparerX.slnx --no-build -m:1
```

Codex sandbox note: MSBuild may fail with named-pipe permission errors. If so,
rerun build/test with elevated permissions.

## Development Rules

- Keep UI out of parsing/domain logic.
- Add tests before reshaping migrated WC behavior.
- Avoid global mutable workspace state.
- Do not commit `bin/`, `obj/`, full client files, or local-only fixture data.
- Add or update docs when architectural boundaries change.
- Add short iteration notes under `docs/logs/`.

