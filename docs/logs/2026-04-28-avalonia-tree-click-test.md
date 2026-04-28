# 2026-04-28 - Avalonia Tree Click Test

## Context

The App test harness already checked programmatic TreeView selection. For M4 it
is more useful to know that a real user-style click on a visible resource node
updates the same selection model and right-side details.

## Changes

- Added a headless mouse interaction smoke test.
- The test loads the synthetic fixture, captures an initial frame to realize
  visible tree containers, clicks the visible `Character.wz` TreeView item, and
  verifies that selected metadata updates to that directory node.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1`

## Notes

- This confirms the current headless harness can test real window-internal
  pointer interactions. Native file picker dialogs should still be covered at
  the ViewModel/service boundary rather than by trying to automate the OS
  dialog.
