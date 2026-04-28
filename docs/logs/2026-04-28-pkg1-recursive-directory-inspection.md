# 2026-04-28 - PKG1 Recursive Directory Inspection

## Summary

- Changed PKG1 directory inspection from top-level-only enumeration to recursive
  directory-table enumeration.
- Added depth and path metadata to inspection entries.
- Updated text output to show `totalEntries` when recursive entries are present
  and indent child entries.
- Added deterministic WzLib coverage for a nested synthetic PKG1 directory tree.

## Notes

WC stores child directory tables linearly after the current directory table.
WCX now reads the same shape instead of only skipping child tables for version
detection. This still stops at directory/image metadata; IMG payload parsing is
left for a later migration step.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect path/to/Data/Base/Base.wz
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect path/to/Data/Base/Base_000.wz
```
