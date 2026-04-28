# 2026-04-28 - Avalonia Tree Expansion

## Context

The M4 browser could load resources, but the tree opened with only the root
node visible. A browser should expose the first level immediately after load so
users can start scanning packages or directories without an extra click.

## Changes

- Added expansion state to `ResourceInspectionNodeViewModel`.
- Default-expanded the loaded root node when it has children.
- Bound `TreeViewItem.IsExpanded` to node state with `ReflectionBinding` so it
  is not miscompiled against the window view model data type.
- Added a ViewModel test assertion that the root expands by default while child
  nodes remain collapsed.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- `WCX_HEADLESS_SCREENSHOT_DIR=/private/tmp/wcx-headless-screens dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1`

## Notes

- The exported headless screenshot shows the synthetic root expanded with
  `Character.wz` and `String.wz` visible.
