# 2026-04-28 - Avalonia Activity Tab

## Context

The activity log was present in the ViewModel, but the right-side layout divided
Document, Selection, Diagnostics, and Activity into four vertical panels. On
normal window sizes, the Activity panel could be squeezed until it appeared
empty or invisible.

## Changes

- Replaced the lower-right stacked Selection, Diagnostics, and Activity panels
  with a `TabControl`.
- Kept Document metadata visible above the tabs.
- Activity now has an explicit tab, making log entries discoverable without
  needing extra vertical space.

## Notes

- The activity log model and parser behavior are unchanged.
