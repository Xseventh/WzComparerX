# 2026-04-29 - Canvas Preview Zoom Controls

## Summary

Made Canvas preview scaling visible and user-controllable.

## Changes

- Raised the auto preview target from a longest side of `160px` to `320px`.
  A `96x60` Canvas now auto-displays at `3x` (`288x180`), while smaller
  character-sized Canvas values such as `45x74` display at `4x`.
- Added Preview tab scale controls: `Auto`, `1x`, `2x`, `4x`, `8x`, and
  `16x`.
- Added a visible scale label such as `Auto (4x)` or `8x`.
- Kept scaling display-only: decoded Canvas pixels remain at original size.
- Used nearest-neighbor interpolation for scaled previews to keep pixel art
  crisp.
- Added ViewModel tests for auto scale, manual scale, and scale command
  behavior.
- Extended the headless Preview screenshot test to assert the scale label and
  Auto button.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
WCX_HEADLESS_SCREENSHOT_DIR=/tmp/wcx-zoom-controls dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build --filter MainWindow_ShowsCanvasPreviewInHeadless
```

Results:

- Build passed with 0 warnings and 0 errors.
- Tests passed: App 45, Core 53, WzLib 44; total 142.
- Headless Preview screenshot smoke passed and rendered the zoom control row.
