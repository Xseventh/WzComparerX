# 2026-04-28 - Basic Avalonia Browser Shell

## Summary

Started Milestone 4 with a minimal Avalonia resource browser that reuses Core
inspection models.

## Changes

- Replaced the greeting-only shell with a path-based resource loader.
- Added a resource tree bound to `ResourceInspectionNode` projections.
- Added document metadata, selected-node metadata, and selected diagnostics
  panels.
- Kept parsing out of the app layer; the view model calls
  `ResourceInspectionService`.
- Added a view-model test using the synthetic fixture.

## Notes

This is the first UI slice, not the final browser workflow. A native file/folder
picker and richer task/log handling are still pending M4 work.

## Verification

- `dotnet restore WzComparerX.slnx`
- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
