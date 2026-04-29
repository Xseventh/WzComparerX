# 2026-04-29 - Canvas Preview

## Summary

Started the Basic Avalonia Browser image display path by mirroring WC's lazy
Canvas behavior.

## Changes

- Added `ResourceCanvasImageService` in Core to load a selected Canvas value
  from an already selected IMG selector.
- Kept Canvas payload decode separate from inspection: directory/package trees
  are eager, IMG inspection is lazy/full, and Canvas pixels are decoded only
  when the UI selects a Canvas value.
- Added a small App bitmap factory that wraps Core BGRA pixels in an Avalonia
  `WriteableBitmap`.
- Added a Preview tab to the main window.
- Selecting a nested Canvas property node now loads a preview. Root Canvas IMG
  objects are supported through the same path without a value selector.
- Added BGRA4444-to-BGRA8888 conversion for format `1`, matching the most
  common small UI Canvas shape observed in local GMS `UI_000.wz/Basic.img`.
- Updated Canvas diagnostics from "not implemented" to "lazy / partial" and
  added viewer-source diagnostics for unsupported preview requests.
- Added Core, ViewModel, and Avalonia Headless tests for Canvas preview.

## Current Scope

- Supported: direct-zlib Canvas format `1` / `2` with scale `0`.
- Not supported yet: PNG export, chunked encrypted Canvas payloads, DXT/BC7,
  scale expansion, multi-page preview controls, UOL-linked preview.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
WCX_HEADLESS_SCREENSHOT_DIR=/tmp/wcx-canvas-preview dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build --filter MainWindow_ShowsCanvasPreviewInHeadless
```

Results:

- Build passed with 0 warnings and 0 errors.
- Tests passed: App 34, Core 53, WzLib 44; total 131.
- Screenshot smoke passed for `MainWindow_ShowsCanvasPreviewInHeadless`.

The screenshot smoke writes:

```text
/tmp/wcx-canvas-preview/main-window-canvas-preview-1100x720.png
```
