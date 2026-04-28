# 2026-04-28 - Avalonia Image Selector Normalization

## Context

The M4 Avalonia browser can display package directory trees where the root node
path includes the package file name, for example
`Base_000.wz/StandardPDD.img`. Core image inspection still uses package-internal
IMG selectors such as `StandardPDD.img`, matching the CLI behavior.

## Changes

- Added App-layer image selector normalization for package-root-prefixed tree
  paths.
- Applied the same normalization to manually entered IMG selectors and the
  `Inspect Image` action.
- Added deterministic ViewModel tests covering root-prefixed, case-insensitive,
  already-normalized, and blank selectors.

## Notes

- Parser and Core selector semantics are unchanged.
- The UI remains an adapter over the Core inspection model instead of moving
  package path behavior into WzLib.
