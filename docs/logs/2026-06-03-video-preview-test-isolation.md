# 2026-06-03 - Video Preview Test Isolation

## Context

The new App video preview tests passed when the whole App test suite ran, but
failed when run alone because `WriteableBitmap` needs an initialized Avalonia
headless platform. That made the ViewModel appear stuck at `Loading Video
preview...`.

## Changes

- Marked video bitmap tests with `AvaloniaFact` so they initialize Avalonia
  Headless when run in isolation.
- Added a ViewModel regression test for video preview factory failures so
  background task faults update the Preview status and activity log instead of
  leaving the UI in a loading state.
- Added a catch-all video preview failure path to mirror the existing Canvas
  preview behavior.

## Notes

This fixes the App test isolation issue. Real GMS video playback still depends
on VPDecoder support for the required VP9 inter-frame features or a future
native decoder backend.
