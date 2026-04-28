# 2026-04-28 - Canvas Export Diagnostics

## Summary

- Added stable Canvas export diagnostic codes for unsupported compression,
  unsupported format, and decode failure.
- Routed Canvas export failures through structured `ResourceExportException`
  diagnostics instead of the generic export unsupported diagnostic.
- Added CLI tests for each Canvas export failure path.
- Updated diagnostics and Canvas export planning docs with the new codes.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
