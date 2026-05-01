# 2026-05-02 MS Container Diagnostic

## Summary

- Reviewed WC `Ms_File` and `Ms_FileV2` enough to confirm `.ms` packs are a
  separate encrypted container path, not WZ files with a different extension.
- Added a stable Core inspection diagnostic for `.ms` container paths.
- `inspect --debug <file>.ms` now returns an inspection document with format
  `ms` and error code `wcx.package.ms.directoryUnsupported` instead of falling
  through to a generic invalid WZ error.

## Notes

- This is not `.ms` parsing yet.
- The next implementation step should choose the smallest WC-aligned `.ms`
  header/entry inspection slice from `Ms_File` / `Ms_FileV2`.

## Verification

- Passed: `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj -m:1`
  - 76 total, 0 failed.
- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 177 total, 0 failed.
- Passed with local GMS data:
  `WCX_GMS_DATA_DIR="<local GMS Data directory>" dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~ResourceInspectionGmsSmokeTests`
  - 3 total, 0 failed.
- Passed: `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless`
  - 13 total, 0 failed.
