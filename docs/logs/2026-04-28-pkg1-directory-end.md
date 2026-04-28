# 2026-04-28 - PKG1 Directory End Validation

## Summary

- Aligned PKG1 version detection with WC's directory pre-read boundary.
- WCX now recursively skips child directory tables before validating calculated
  directory offsets.
- Restored strict directory-offset validation against the computed directory
  table range.
- Updated synthetic WZ tests to include empty child directory count stubs.

## Notes

WC's `Pkg1PreReader` reads child directory tables linearly after each directory
level, before version validation. The local MapleStoryNA `Data/Base/Base.wz`
therefore has a directory table ending at file position `376`: the top-level
table ends at `360`, followed by 16 empty child directory count bytes. The
calculated offsets `360` through `375` are inside that recursively computed
directory-table range, so WCX should not use a broader "any in-file offset"
rule.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect path/to/Data/Base/Base.wz
```
