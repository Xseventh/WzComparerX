# 2026-04-28 - CLI Depth Regression Test

## Summary

- Added CLI regression coverage for `inspect --debug --depth 1` on a synthetic
  PKG1 Canvas IMG with mini-property metadata.
- Locked the user-visible behavior that debug output keeps `childCount` and
  Canvas payload metadata while omitting the mini-property child node at the
  requested depth.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
