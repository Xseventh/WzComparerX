# 2026-04-28 - Export JSON Boundary

## Summary

Clarified that `export` writes resource content directly and does not support
the global `--json` formatter flag.

## Changes

- Added an explicit CLI error for `export --json`.
- Added a deterministic CLI test and expected stderr fixture.
- Updated command docs to state that metadata export is already JSON and other
  export kinds define their own content format.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
