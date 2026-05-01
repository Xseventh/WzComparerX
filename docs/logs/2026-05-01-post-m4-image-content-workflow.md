# 2026-05-01 Post-M4 IMG Content Workflow

## Summary

- Continued the post-M4 cleanup called out in the roadmap.
- Extracted IMG Content workflow decisions out of `MainWindowViewModel` into an
  App helper:
  - selected image node target resolution;
  - manual selector target resolution;
  - repeated current target detection;
  - Core full-IMG inspection calls.
- Kept ViewModel ownership of visible state, request ordering, selector text
  updates, and activity log messages.

## Notes

This mirrors the previous Canvas Preview workflow split. The Resources tree,
IMG Content tree, and Canvas Preview behavior should remain unchanged: selecting
an image node still lazily loads that single IMG into IMG Content, and Canvas
Preview still follows IMG Content selection.

The next cleanup candidates are now smaller:

- selection/detail panel orchestration in `MainWindowViewModel`;
- Core package group / split-package linking helper boundaries;
- the first narrow Milestone 5 Compare Foundation model.

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
- Full test passed: 153 total, 0 failed, 0 skipped.
