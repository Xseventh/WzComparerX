# 2026-04-28 - Avalonia Open Entry Alignment

## Context

WC exposes resource loading primarily through `Open Wz...` and a separate
`Open Img...` entry, not as parallel file and folder buttons. WCX briefly had
separate `Browse` and `Folder` buttons during M4 folder-browser bring-up.

## Changes

- Removed the top-level `Folder` button from the path row.
- Kept one visible `Browse` entry with menu choices for `Open Wz...` and
  `Open Folder...`.
- Kept folder inspection support through `Path` plus `Load`, where the ViewModel
  already detects whether the path is a file or directory.
- Updated M4 handoff and roadmap wording to describe folder handling as a load
  behavior, not a separate main UI entry.

## Notes

- The Core folder inspection service remains because it is useful for a typed or
  remembered client `Data` directory path.
- A later WC-aligned `Open Img...` entry should be considered when standalone
  IMG file loading becomes a supported parser path.
