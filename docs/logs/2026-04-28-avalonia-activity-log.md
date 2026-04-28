# 2026-04-28 - Avalonia Activity Log

## Context

The M4 browser had a single status line, but no history of load, inspection, or
error events. A small log panel makes repeated package and image browsing easier
to follow without changing parser behavior.

## Changes

- Added a ViewModel activity log collection.
- Logged load start, successful file/folder/image loads, and validation or IO
  errors.
- Added an Activity panel to the main window.
- Added deterministic ViewModel assertions for activity log entries.

## Notes

- Log entries deliberately omit timestamps so tests and UI output stay stable.
- This is a lightweight M4 browser log, not a long-running task/progress model.
