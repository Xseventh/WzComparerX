# 2026-05-02 PKG1 Directory Edge Guards

## Summary

- Tightened PKG1 directory inspection around malformed directory-table inputs.
- Negative compressed directory entry counts now fail explicitly instead of being
  treated as an empty directory.
- PKG1 `0x02` string-reference names now report an explicit invalid-data error
  when the resolved string offset points beyond the file.
- Added deterministic WzLib tests for both edge cases.

## Verification

- Passed: `dotnet test tests/WzComparerX.WzLib.Tests/WzComparerX.WzLib.Tests.csproj -m:1`
  - 46 total, 0 failed.
- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 172 total, 0 failed.
- Passed: `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless`
  - 13 total, 0 failed.
