# 2026-04-28 - Avalonia Folder Browser

## Context

M4 needs the desktop app to browse from a client `Data` directory toward
individual WZ packages without copying paths by hand.

## Changes

- Added a Core folder inspection service that scans WZ package headers and
  projects them into the shared inspection document/tree model.
- Added an Avalonia folder picker.
- Added an `Open Package` action for package nodes discovered by folder
  inspection.
- Added deterministic Core and ViewModel tests for folder package projection and
  package opening.

## Notes

- Folder inspection only lists discovered `.wz` packages and header metadata; it
  does not parse package directory contents until a package node is opened.
- The App layer continues to render Core inspection documents rather than
  owning parser behavior.
