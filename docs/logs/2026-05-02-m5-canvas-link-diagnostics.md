# 2026-05-02 M5 Canvas Link Diagnostics

## Summary

- Added `wcx.viewer.canvas.linkUnresolved` for Canvas preview link targets that
  cannot be resolved.
- Kept `source`, `_inlink`, and `_outlink` preview semantics aligned with WC:
  `_inlink` searches the current IMG and external links search the workspace
  layout when it can be mapped.
- Preserved the existing unsupported-value diagnostic for values that are not
  Canvas values or Canvas links.
- Added deterministic Core coverage for an unresolved `_outlink` target.

## Behavior

Selecting a Canvas link string such as `proxy/_outlink` now reports a specific
viewer error when the target path cannot be mapped to a package, IMG selector,
and Canvas value:

```text
error [wcx.viewer.canvas.linkUnresolved]: Canvas preview _outlink target could not be resolved: Map/Missing/Missing.img/icon. (Proxy.img/proxy/_outlink)
```

This makes link-resolution failures distinct from unsupported Canvas formats or
non-Canvas selections.

## Verification

```text
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed. Test count: 160 total, 0 failed.
