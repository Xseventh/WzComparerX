# 2026-05-01 M5 Resource Identity

## Summary

- Added `ResourceInspectionIdentity` to Core inspection nodes.
- Populated identity for package roots, directory/image entries, IMG roots, and
  IMG property/value nodes.
- Locked merged shard image identity so a merged image node records the shard
  package path and selector separately from its displayed resource-tree path.
- Updated the Canvas debug JSON golden output to include stable identity data.

## Behavior

The inspection tree now carries structured identity fields:

- `PackagePath`: the package that owns the node or selected IMG value.
- `ImageSelector`: the IMG selector when the node represents an image or a value
  inside an image.
- `ValuePath`: the inside-IMG property/value path for IMG content nodes.
- `LinkedTarget`: reserved for M5 link-resolution work.

Text inspect output remains compact; the new identity data is visible through
the structured model and JSON output.

## Verification

```text
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed. Test count: 156 total, 0 failed.

## Next

- Move `source` / `_inlink` / `_outlink` / UOL link target metadata into Core
  inspection identity or diagnostics.
- Add diagnostics for unresolved split-package and linked Canvas targets.
