# 2026-04-28 - PKG1 Version And Offset Preview

## Summary

- Added PKG1 hash-version calculation and encrypted-version matching.
- Added PKG1 entry offset calculation for directory preview entries.
- Updated text and JSON preview output with `wzVersion`, `hashVersion`, and
  per-entry `Offset` when detection succeeds.
- Added deterministic WzLib tests for version hashing, offset calculation,
  missing-encrypted-version files, and encrypted-version directory stub offsets.

## Local Smoke

Verified the local MapleStoryNA `Data/Base/Base.wz` with:

```bash
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --key none path/to/Data/Base/Base.wz
```

The file resolves as WZ version `264`, hash version `54037`, with top-level
directory offsets `360` through `375`.

## Notes

The local split-package style stores top-level empty child directory tables
immediately after the top-level directory table. WCX now mirrors WC's pre-read
shape: recursively skip child directory tables to compute the directory-table
end, then require directory offsets to land within that computed range.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```
