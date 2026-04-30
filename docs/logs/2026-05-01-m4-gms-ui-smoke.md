# 2026-05-01 M4 GMS UI Smoke

## Summary

- Updated project progress docs for the current remote layout and M4 closeout
  state.
- Added an optional Avalonia Headless smoke test for a local GMS
  `Map/Map/Map1/Map1.wz` package group.
- Recorded the M4 closeout decision that direct-zlib Canvas preview formats `1`
  and `2` are enough for the Basic Avalonia Browser milestone. Broader Canvas
  format coverage, PNG export, and MapRender stay in later milestones.
- Deferred larger `MainWindowViewModel` and Core package-group helper cleanup
  until after M4 closeout so the UI smoke can stabilize first.

## Local GMS Smoke

The local test used `WCX_GMS_DATA_DIR` pointing at the user's GMS `Data`
directory. The client files are not committed.

Sample category:

```text
Data/Map/Map/Map1/Map1.wz
```

Validated behavior:

- Opening `Map1.wz` uses the WC-style package group loader.
- `Map1_000.wz` entries merge under the `Map1.wz` Resources tree.
- The merged `100000000.img` node keeps a target path that resolves back to
  `Map1_000.wz/100000000.img`.
- Selecting `100000000.img` keeps the Resources tree in place and fills the
  separate IMG Content tree with the full IMG.
- `miniMap/canvas` is a 1x1 placeholder in this sample; selecting the
  `_outlink` string resolves to the real Canvas through the workspace path.
- Canvas Preview renders the linked `100000000.img/miniMap/canvas` value at
  `364x79`.
- Large directory traversal and large Canvas auto-scale both exercised the M4 UI
  path.

Optional screenshot artifacts from the local run:

```text
/private/tmp/wcx-gms-ui-smoke-2026-05-01/gms-map1-package-group.png
/private/tmp/wcx-gms-ui-smoke-2026-05-01/gms-map1-img-content.png
/private/tmp/wcx-gms-ui-smoke-2026-05-01/gms-map1-canvas-preview.png
```

These screenshots are local-only artifacts and should not be committed.

## Commands

Build:

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
```

Optional local GMS UI smoke:

```bash
WCX_GMS_DATA_DIR="<local GMS Data directory>" \
WCX_GMS_UI_SMOKE_SCREENSHOT_DIR="/private/tmp/wcx-gms-ui-smoke-2026-05-01" \
dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj \
  --no-build \
  --filter MainWindow_OptionalGmsMapPackageGroupSmoke \
  -m:1
```

Full test:

```bash
dotnet test WzComparerX.slnx --no-build -m:1
```

## Results

- Build passed.
- Optional GMS UI smoke passed.
- Full test passed: 150 total, 0 failed, 0 skipped.

## Follow-Ups

- M4 closeout should stay focused on documentation and final smoke review.
- After M4 closeout, split `MainWindowViewModel` workflow responsibilities into
  smaller UI workflow/helper services.
- After M4 closeout, review the Core package-group / split-linking boundary and
  extract helper types only where it makes the next behavior easier to test.
