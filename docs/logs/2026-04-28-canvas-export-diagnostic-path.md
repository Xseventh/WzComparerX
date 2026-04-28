# 2026-04-28 - Canvas Export Diagnostic Path

## Summary

- Changed Canvas export diagnostics to report the selected Canvas value path
  when available.
- Kept unsupported export diagnostics at the image selector when no Canvas value
  is selected.
- Updated CLI assertions for unsupported Canvas compression, unsupported format,
  and decode failure diagnostics.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
