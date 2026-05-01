# 2026-05-01 M5 Split Package Diagnostics

## Summary

- Added `wcx.package.link.unresolved` for failed split-package candidate loads.
- Added the `inspection` diagnostic source for Core inspection composition and
  workspace/resource-linking conditions.
- Attached unresolved split-package diagnostics to the affected directory stub
  when Core found a candidate package path but could not load it.
- Kept missing split-package candidates silent to avoid treating every empty
  directory stub as a warning.
- Added deterministic Core coverage for an invalid linked package candidate.

## Behavior

When an empty directory stub points to an existing sibling candidate such as
`Effect/Effect.wz`, but that package is invalid or unreadable, `inspect
--debug` now reports:

```text
warning [wcx.package.link.unresolved]: Split-package link could not be resolved for directory stub: Effect. (Effect)
```

This is intentionally attached to the stub node rather than the document root so
future UI/Search/Compare workflows can keep the problem local to the affected
resource path.

## Verification

```text
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed. Test count: 158 total, 0 failed.
