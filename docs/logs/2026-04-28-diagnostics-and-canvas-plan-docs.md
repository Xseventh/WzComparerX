# 2026-04-28 - Diagnostics And Canvas Plan Docs

## Summary

- Added `docs/diagnostics.md` to document diagnostic severity, source, code,
  text formatting, JSON shape, and addition rules.
- Added `docs/canvas-decode-export-plan.md` to define the first narrow Canvas
  decode/export loop and fixture strategy.
- Updated README, roadmap, commands, and handoff to point at the new docs.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
