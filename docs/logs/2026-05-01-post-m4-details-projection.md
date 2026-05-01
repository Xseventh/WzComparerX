# 2026-05-01 Post-M4 Details Projection

## Summary

- Continued post-M4 `MainWindowViewModel` cleanup.
- Extracted Document / Selection / Diagnostics panel projection into a small
  ViewModel helper.
- Added focused tests for:
  - document source / format / debug metadata projection;
  - selected node identity / optional values / debug metadata projection;
  - selected diagnostic projection.

## Notes

This is a behavior-preserving cleanup. The UI still owns visible collections
and property notifications, but the rules for turning Core/App resource models
into detail-panel rows now live outside the main window orchestration.

The remaining obvious UI cleanup is activity/progress orchestration. Core
package group and split-package linking boundaries should also be reviewed
before starting broad compare/search features.

## Verification

Build:

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
```

Full test:

```bash
dotnet test WzComparerX.slnx --no-build -m:1
```

Results:

- Build passed.
- Full test passed: 156 total, 0 failed, 0 skipped.
