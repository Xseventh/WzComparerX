# 2026-04-28 - Avalonia Image Activation Test

## Context

M4 can load WZ package directories and inspect an IMG when the selector is set.
The browser flow also needs to support selecting an image node from the resource
tree and activating it through the same ViewModel path used by double-click.

## Changes

- Added an App ViewModel test using the committed `canvas-zlib.pkg1.hex`
  fixture materialized into a temporary `.wz` file.
- The test loads the package directory, selects `Canvas.img`, activates the
  selected node, and verifies that the UI state changes to the image inspection
  document.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1`
