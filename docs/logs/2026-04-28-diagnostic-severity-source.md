# 2026-04-28 - Diagnostic Severity and Source Constants

## Summary

- Added `ResourceDiagnosticSeverities` for stable diagnostic severity strings.
- Added `ResourceDiagnosticSources` for stable diagnostic source strings.
- Updated shared diagnostic factories and tests to use the named constants.
- Updated handoff to describe the current diagnostic boundary.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
