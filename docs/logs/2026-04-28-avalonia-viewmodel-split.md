# 2026-04-28 - Avalonia ViewModel Split

## Summary

Continued M4 cleanup by reducing `MainWindowViewModel` as the browser shell
starts to grow.

## Changes

- Moved IMG selector normalization into `WzComparerX.App.Services`.
- Split resource node, metadata, diagnostics, and activity log projection
  models into dedicated App ViewModel files.
- Kept the main window view model focused on command state, load/inspect
  orchestration, selection, and visible status.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
