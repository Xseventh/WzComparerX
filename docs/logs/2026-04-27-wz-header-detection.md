# 2026-04-27 WZ Header Detection

## Summary

Started Milestone 2 by migrating the smallest real WC parser behavior: WZ
package header detection.

## Completed

- Added `WzPackageHeaderReader` in `WzComparerX.WzLib`.
- Added explicit `WzPackageHeader` and `WzPackageFormat` models.
- Preserved WC's `PKG1`/`PKG2` header field layout.
- Preserved WC's PKG1 encrypted-version-missing heuristic.
- Added deterministic tests for valid PKG1, missing PKG1 encver, valid PKG2,
  and invalid signatures.
- Updated migration and format notes with supported and unsupported behavior.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```

## Next Suggested Work

Continue Milestone 2 by adding either WZ version profile detection or the first
directory tree enumeration path, preferably behind another tiny fixture-backed
test.
