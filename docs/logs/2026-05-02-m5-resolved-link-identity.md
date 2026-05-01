# 2026-05-02 M5 Resolved Link Identity

## Summary

- Added a shared Core link resolver for deterministic IMG link identity.
- `inspect --debug` now can populate `Identity.ResolvedLinkedTarget` for:
  - `_inlink` values inside the current IMG;
  - relative UOL values inside the current IMG;
  - logical `source`, `_outlink`, and `link` targets that include an `.img`
    segment and resolve through the current `Data` workspace package groups.
- Canvas preview now reuses the same resolver instead of keeping a separate
  logical package lookup path.

## Behavior

- Normalized raw link text remains in `Identity.LinkedTarget`.
- Resolved link identity records package path, image selector, and optional
  inside-IMG value path.
- The extra cross-package lookup is limited to debug inspection and preview
  workflows; ordinary `inspect` still avoids the additional read-only package
  group work.

## Verification

- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 167 total, 0 failed.
