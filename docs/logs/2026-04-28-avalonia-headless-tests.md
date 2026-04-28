# 2026-04-28 - Avalonia Headless Tests

## Context

M4 UI changes were covered by ViewModel tests inside `WzComparerX.Core.Tests`,
which made Core.Tests depend on the App project. UI-specific tests should live
with an App test harness and should be able to exercise real Avalonia controls.

## Changes

- Added `WzComparerX.App.Tests`.
- Moved `MainWindowViewModelTests` from Core.Tests into App.Tests.
- Removed the App project reference from Core.Tests.
- Added `Avalonia.Headless.XUnit` and xUnit v3 test support for App.Tests.
- Added a first Avalonia Headless smoke test that constructs `MainWindow` and
  verifies the resource tree plus Selection, Diagnostics, and Activity tabs.

## Verification

- `dotnet restore WzComparerX.slnx`
- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`

## Notes

- NuGet restore needed network access for the new headless/xUnit packages.
- Headless screenshot capture is still a follow-up; this iteration establishes
  the test project and basic control-structure smoke coverage first.
