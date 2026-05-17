# 2026-05-17 UI Outlink App Smoke

## Context

The previous UI `_outlink` slice proved the Core inspection and Canvas preview
service path for `Data/UI/UI_000.wz` selector `Basic.img`. M5 also wants App
workflows to stay on the same Core path, especially after the Resources tree and
IMG Content tree were split to match WC browsing behavior.

## Changes

- Added an optional Avalonia Headless external-client smoke for
  `Data/UI/UI_000.wz`.
- The smoke opens the package, selects `Basic.img` from Resources, waits for the
  full IMG Content tree, selects `Cursor/0/0/_outlink`, and verifies the Preview
  tab loads the linked 24x28 format-1 Canvas.
- The screenshot hook can save `gms-ui-basic-outlink-canvas-preview.png` when
  `WCX_EXTERNAL_CLIENT_UI_SMOKE_SCREENSHOT_DIR`,
  `WCX_GMS_UI_SMOKE_SCREENSHOT_DIR`, or `WCX_HEADLESS_SCREENSHOT_DIR` is set.

## Validation

Run the targeted smoke with a local client `Data` directory:

```bash
WCX_CLIENT_DATA_DIR=/path/to/Data dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~MainWindow_OptionalExternalClientUiOutlinkCanvasSmoke
```

The targeted smoke passed against the local GMS client. Full solution build/test
and the standard App Headless subset also passed before commit.
