# 2026-05-01 M5 Link Identity

## Summary

- Populated `ResourceInspectionIdentity.LinkedTarget` for link-like IMG value
  nodes.
- Added debug metadata for `linkKind` and `linkedTarget` on `source`,
  `_inlink`, `_outlink`, `link`, and UOL nodes.
- Added deterministic Core coverage for `_outlink` string values and UOL object
  values.
- Fixed the Core test helper so synthetic PKG1 image sizes use the same
  compressed-int encoding as the reader, allowing larger generated IMG
  fixtures.

## Behavior

Core inspection now represents the raw normalized link target without doing
extra package IO during normal IMG projection. This keeps `inspect` fast and
deterministic while making links visible to future Compare/Search/export
consumers.

The next step is resolved-link diagnostics: unresolved split-package and
Canvas/link targets should produce stable diagnostics rather than silent
omissions.

## Verification

```text
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed. Test count: 157 total, 0 failed.
