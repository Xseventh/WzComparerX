# 2026-04-28 - Avalonia File Picker

## Context

M4 is moving the browser from command-line-only workflows toward a usable
desktop shell while keeping parsing and inspection behavior in Core.

## Changes

- Added a native Avalonia file picker button for WZ packages and synthetic JSON
  fixtures.
- Routed picked files through the existing ViewModel load path.
- Clear the previous IMG selector when opening a new file from the picker so an
  old image selection does not accidentally apply to the next package.
- Added a ViewModel test for the new open-path state behavior.

## Notes

- Folder picker workflows are still pending.
- The App layer still delegates resource loading to `ResourceInspectionService`.
