# 2026-04-28 - Avalonia Headless Screenshot Tests

## Context

M4 has an Avalonia Headless test harness, but it only checked window/control
structure. The next useful step is render-backed coverage so UI changes can
catch blank frames and basic layout regressions without launching the desktop
app manually.

## Changes

- Enabled Skia-backed headless drawing in App.Tests with `UseHeadlessDrawing =
  false`.
- Added stable names to primary `MainWindow` controls so headless layout tests
  can target user-visible UI elements directly.
- Added a headless screenshot smoke test that loads the synthetic fixture,
  captures the rendered frame, verifies PNG saving, and checks that the pixel
  data has varied rendered content.
- Added viewport layout checks for the default M4 window size and a compact
  window size.
- Closed headless windows after each test to keep the test session isolated.
- Added an opt-in `WCX_HEADLESS_SCREENSHOT_DIR` artifact path for writing the
  captured PNG outside the repository during manual visual checks.
- Moved Activity out of the details tabs into a persistent lower-right panel so
  load/error logs are visible without selecting a tab.
- Added a headless interaction smoke test that switches the details tab and
  captures a rendered frame after the control state changes.

## Findings

- `CaptureRenderedFrame` returned `null` until the test application explicitly
  enabled Skia-backed headless drawing. This was a test harness gap rather than
  an app UI bug.
- The current primary controls stay inside both the default `1100x720` and a
  compact `900x640` headless viewport.
- Manual artifact generation produced
  `/private/tmp/wcx-headless-screens/main-window-synthetic-1100x720.png`.
- The updated artifact confirms that Activity entries are visible in the
  lower-right panel.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1`
- `WCX_HEADLESS_SCREENSHOT_DIR=/private/tmp/wcx-headless-screens dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1`
