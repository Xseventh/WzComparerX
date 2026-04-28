# 2026-04-28 - Canvas Export Value Selector

## Summary

Implemented explicit Canvas value selection for raw Canvas byte export.

## Changes

- Added `--value <property-path>` to `export`.
- Added `ResourceExportOptions.ValueSelector`.
- Canvas export now requires `--value` unless the selected IMG root object is a
  Canvas.
- Added stable export diagnostics for required, missing, unsupported, and
  ambiguous value selectors.
- Updated Canvas export docs and command examples.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
