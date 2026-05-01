# 2026-05-01 Post-M4 Split Package Resolver

## Summary

- Continued post-M4 Core boundary cleanup.
- Extracted split-package link path resolution out of `ResourceInspectionService`
  into a dedicated Core helper.
- Kept the behavior unchanged:
  - same-package relative split directory lookup;
  - conservative `Base/Base.wz` workspace-relative fallback;
  - package group entry path enumeration;
  - candidate de-duplication;
  - split-package ancestor path normalization.

## Notes

`ResourceInspectionService` still owns inspection tree composition and linked
package grafting. The new helper only owns filesystem/path policy, which keeps
WC-style split-package lookup easier to review before M5 compare/search starts
depending on the same resource tree.

The next Core cleanup candidate is the inspection node builder / package group
tree composition boundary. That should stay behavior-preserving unless M5 needs
a more explicit reusable tree model.

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
