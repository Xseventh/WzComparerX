# 2026-05-17 MS Outlink Headless Smoke

- Added an optional Avalonia Headless smoke test for the real local GMS
  `Packs/Mob_00000.ms` workflow.
- The test opens the `.ms` container, selects `Mob/1150000.img`, loads IMG
  Content, selects `move/0/_outlink`, and verifies the Preview tab renders the
  linked Canvas instead of treating the MS package path as a WZ package.
- This locks the App side of the same `_outlink` path already covered by the
  Core external-client smoke.

Validation:

- `WCX_CLIENT_DATA_DIR=<local GMS Data> dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~ResourceInspectionExternalClientSmokeTests`
  passed: 14/14.
- `WCX_CLIENT_DATA_DIR=<local GMS Data> WCX_HEADLESS_SCREENSHOT_DIR=/private/tmp/wcx-m5-headless-smoke dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj -m:1 --filter FullyQualifiedName~MainWindow_OptionalExternalClientMsOutlinkCanvasSmoke`
  passed: 1/1.
- Headless screenshot artifact:
  `/private/tmp/wcx-m5-headless-smoke/gms-ms-outlink-canvas-preview.png`
