# 2026-04-28 - Avalonia Tree Selection Test

## Context

M4 now has Skia-backed headless screenshots and the resource root expands after
load. The next useful interaction check is selection flow: choosing a resource
node should update the ViewModel and the right-side Selection panel data.

## Changes

- Added a headless TreeView selection smoke test.
- The test loads the synthetic fixture, selects the visible `Character.wz`
  child through `ResourcesTree.SelectedItem`, captures a rendered frame, and
  verifies selected-node metadata.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1`
