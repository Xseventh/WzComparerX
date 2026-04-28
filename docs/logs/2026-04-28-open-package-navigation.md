# 2026-04-28 - Open Package Navigation

## Summary

Made the M4 browser navigation less modal after inspecting an IMG entry.

## Changes

- Renamed the App command from selected-package-only semantics to generic
  package opening semantics.
- `Open Package` still opens a package node selected from a folder inspection.
- When the current view is an IMG inspection, `Open Package` clears the IMG
  selector and reloads the current package directory.
- Renamed the button control to `OpenPackageButton`.
- Added ViewModel coverage for returning from IMG inspection to package view and
  adjusted headless button-state checks.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
