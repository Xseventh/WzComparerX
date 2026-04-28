# 2026-04-28 - Diagnostic Factory

## Summary

- Added `ResourceInspectionDiagnostics` as the shared factory for stable
  inspection/export diagnostics.
- Routed parser unsupported-payload diagnostics and export diagnostics through
  the shared factory.
- Added Core tests for stable payload/export diagnostic severity, message,
  code, source, and path fields.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
