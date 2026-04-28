# 2026-04-28 - Avalonia Image Inspection

## Summary

Extended the basic Avalonia browser so selected image entries can be inspected
through the existing Core inspection service.

## Changes

- Added IMG selector, string-key, and depth inputs to the main window.
- Added an `Inspect Image` action that uses the selected image node path as the
  IMG selector.
- Kept image inspection routed through `ResourceInspectionService`.
- Added view-model tests for invalid key/depth input behavior.

## Notes

This keeps M4 aligned with WC's tree-first browsing workflow while preserving
the WCX boundary: UI commands choose resources, Core performs inspection.
Native file/folder picker integration is still pending.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
