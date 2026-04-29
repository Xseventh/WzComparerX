# 2026-04-29 IMG Control Cleanup

## Summary

Tightened the M4 Avalonia IMG browsing controls after the WC-style IMG Content
tree landed. The resource tree now owns selected-image loading, and the selector
row is reserved for manual or refresh workflows.

## Changes

- Removed the visible `Open Package` toolbar button. Package nodes can still be
  activated from the resource tree, while normal package/group browsing stays in
  the tree itself.
- Renamed `Inspect Image` to `Load IMG` and narrowed it to manually entered IMG
  selectors.
- Kept automatic selected-image extraction into IMG Content, matching WC's
  lazy/full single-IMG browsing model.
- Updated Avalonia ViewModel and headless tests to assert that selected image
  nodes auto-load content without enabling the manual selector button.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
