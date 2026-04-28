# 2026-04-28 - Diagnostic Formatter

## Summary

- Added `ResourceInspectionDiagnosticFormatter` as the shared text formatter for
  inspection/export diagnostics.
- Routed inspect text output and CLI export stderr diagnostics through the same
  formatter.
- Added deterministic Core tests for code/path formatting and empty code/path
  omission.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
