# 2026-04-28 - Avalonia Node Activation

## Context

The M4 browser could open selected package and image nodes through explicit
buttons, but resource tree browsing should also support direct activation from
the tree.

## Changes

- Added a ViewModel-level selected-node activation command.
- Package nodes activate by opening the selected package.
- Image nodes activate by running the existing image inspection path.
- Wired TreeView double-click activation to the ViewModel command.
- Added a deterministic ViewModel test for package-node activation.

## Notes

- Code-behind only forwards the TreeView event; node-kind decisions stay in the
  ViewModel.
- Parser and Core inspection behavior are unchanged.
