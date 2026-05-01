# 2026-05-02 M5 Resource Identity Contract

## Summary

- Documented the current `ResourceInspectionIdentity` contract for package,
  image selector, inside-IMG value path, and normalized link target fields.
- Added CLI debug JSON coverage so automation can rely on identity records
  instead of parsing display paths.
- Updated the parser coverage matrix to treat identity semantics as documented,
  with richer resolved-target identity left for later M5 slices.

## Behavior

- `ResourceInspectionNode.Path` remains the tree/display path and should not be
  parsed as a filesystem target.
- `Identity.PackagePath` points at the true package owner, including merged
  numbered shard packages.
- `Identity.ImageSelector` is the selector to pass back to Core for IMG
  inspection/export.
- `Identity.ValuePath` identifies an inside-IMG value.
- `Identity.LinkedTarget` stores normalized raw link text for link-like values;
  it is not yet a guaranteed resolved target identity.

## Verification

- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 166 total, 0 failed.
