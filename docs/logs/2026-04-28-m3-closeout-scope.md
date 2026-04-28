# 2026-04-28 - M3 Closeout Scope

## Summary

Captured the M3 stopping point for export, diagnostics, and Canvas work so the
next iteration does not keep expanding scope.

## Decisions

- Raw Canvas byte export is sufficient for M3 as a parser/export slice.
- PNG export is not required for M3 and stays in a later user-facing image
  export milestone.
- Canvas export must gain explicit value selection before M3 closes; relying on
  the first Canvas value is only a temporary implementation shortcut.
- Lua IMG and WC text-format IMG behavior remains covered by synthetic fixtures,
  but direct real-client smoke verification should be added when suitable
  samples are found.
- M4 Basic Avalonia Browser should start after M3 inspect/export/diagnostics
  contracts are stable enough for UI reuse.

## Documentation

- Synced `docs/handoff.md` recent commits and current-state notes.
- Added `docs/canvas-export-selector-plan.md`.
- Updated `docs/roadmap.md`, `docs/commands.md`, and
  `docs/canvas-decode-export-plan.md`.

## Verification

- `dotnet test WzComparerX.slnx --no-build -m:1`
