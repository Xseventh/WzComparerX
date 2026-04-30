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

Do not push to any remote unless the user explicitly asks. The current local
remote layout is:

- `origin`: `Xseventh/WzComparerX`
- `upstream`: `Kagamia/WzComparerX`

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

Post-M4 cleanup and Milestone 5 planning.

Immediate direction:

- Keep the UI on Core inspection/export services.
- Do not add parser behavior to App view models.
- Split `MainWindowViewModel` responsibilities into small App workflow/helpers
  when the behavior is already covered by tests.
- Review Core package group / split-package linking boundaries before expanding
  compare or search workflows.
- Prepare Milestone 5 Compare Foundation with a narrow CLI/test-first plan.

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
