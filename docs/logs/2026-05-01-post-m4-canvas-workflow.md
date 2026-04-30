# 2026-05-01 M4 Closeout And Canvas Workflow

## Summary

- Marked Milestone 4 as complete after the GMS UI smoke passed.
- Updated project docs so the current short-term direction is post-M4 cleanup
  and Milestone 5 Compare Foundation planning.
- Extracted Canvas Preview workflow decisions out of `MainWindowViewModel` into
  an App helper:
  - whether a selected IMG Content node can preview a Canvas;
  - how Canvas, root Canvas IMG, `source`, `_inlink`, and `_outlink` nodes map to
    value selectors;
  - which Core `ResourceCanvasImageService` method to call.

## Notes

The ViewModel still owns visible UI state:

- request ordering and stale request suppression;
- status strings;
- replacing and disposing preview view models;
- diagnostics projection;
- activity log messages.

This keeps the behavior unchanged while making the next UI cleanup smaller. IMG
Content loading and Core package-group / split-package linking are still the next
obvious boundaries to review.

## Verification

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

Results:

- Build passed.
- Optional local GMS UI smoke passed.
- Full test passed: 150 total, 0 failed, 0 skipped.
