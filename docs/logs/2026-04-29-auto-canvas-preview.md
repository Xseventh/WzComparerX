# 2026-04-29 - Auto Canvas Preview

## Summary

Made Avalonia preview feel more like browsing: selecting an IMG node now tries
to show its first Canvas value immediately.

## Changes

- Added `ResourceCanvasImageService.LoadFirstAsync` for preview-only first
  Canvas selection.
- The UI now previews a directory image node without requiring `Inspect Image`
  first.
- Linked package image nodes use the already resolved package/image target, so
  auto preview works for paths shaped like `<package>.wz/<image>.img`.
- Selecting a concrete Canvas value inside an inspected IMG still previews that
  exact Canvas path.
- Added integer display scaling for small Canvas previews. The bitmap remains
  decoded at original size, while the Avalonia `Image` control displays it at a
  capped pixel-art scale with nearest-neighbor interpolation.
- Added ViewModel tests for selected-image auto preview, linked-package auto
  preview, and preview display scaling.

## Current Scope

- Auto preview chooses the first Canvas value in IMG property traversal order.
- Unsupported Canvas formats/compression still surface as viewer diagnostics.
- This does not replace full IMG inspection; `Inspect Image` / double-click
  still loads the full IMG tree for browsing its internal properties.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
WCX_HEADLESS_SCREENSHOT_DIR=/tmp/wcx-auto-preview dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build --filter MainWindow_ShowsCanvasPreviewInHeadless
```

Results:

- Build passed with 0 warnings and 0 errors.
- Tests passed: App 43, Core 53, WzLib 44; total 140.
- Headless Canvas screenshot smoke passed; the synthetic 2x1 Canvas is now
  visibly enlarged in the preview panel.
