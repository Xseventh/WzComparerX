# 2026-04-28 - Avalonia UI State Tests

## Summary

Tightened the M4 browser shell around visible user states and trimmed parsing
responsibility out of the main window view model.

## Changes

- Added an App-level resource inspection option parser for key/depth text input.
- Kept `MainWindowViewModel` focused on loading, selection, command state, and
  UI projection.
- Disabled redundant package open and IMG inspect commands when the selected
  resource already matches the current view.
- Named the status text block so headless tests can assert user-visible status.
- Added Avalonia Headless coverage for invalid path, folder open, package open,
  and image inspection states.
- Moved shared App test fixture helpers into `AppTestFixtures`.
- Synced M4 status notes in roadmap and handoff.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
