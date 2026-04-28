# 2026-04-28 - Manual Image Inspect

## Summary

Made the M4 `Inspect Image` action match the visible IMG selector field.

## Changes

- Renamed the App command from selected-image-only semantics to generic image
  inspection semantics.
- `Inspect Image` now inspects the selected image node when one is selected, or
  the manually entered IMG selector when the selector field has a value.
- Disabled redundant image inspection when the currently opened image already
  matches the selector.
- Renamed the button control from `InspectSelectedImageButton` to
  `InspectImageButton`.
- Added ViewModel and Avalonia Headless tests for manual selector inspection.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
