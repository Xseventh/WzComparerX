# 2026-04-28 - Canvas Raw Export

## Summary

- Added `ResourceExportKind.Canvas` and CLI parsing for `export --type canvas`.
- Added Core export support for the first supported Canvas value in a selected
  IMG.
- Kept Canvas export binary-only: callers must use `--out <path>`.
- Export currently writes raw decoded pixel bytes, not PNG.
- Added CLI tests for successful `--out` export and the binary stdout guard.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
