# 2026-04-28 - Binary Export Out Diagnostic

## Summary

- Added stable diagnostic code `wcx.export.binary.outRequired`.
- Routed binary export without `--out <path>` through the shared diagnostic
  formatter.
- Updated Canvas export CLI coverage and diagnostics docs.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
