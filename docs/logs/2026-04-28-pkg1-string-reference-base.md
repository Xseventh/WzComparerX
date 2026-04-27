# 2026-04-28 - PKG1 String Reference Offset Base

## Summary

- Corrected PKG1 `0x02` string-reference name resolution to add the package
  directory start position.
- Updated the deterministic referenced-name test to encode offsets relative to
  the directory data stream, matching WC's `PartialStream` behavior.

## Notes

WC reads PKG1 directory trees through a `PartialStream` whose position zero is
`Header.DirStartPosition`. `ReadStringAt(reader.ReadInt32() + stringOffAdd)`
therefore seeks to `Header.DirStartPosition + encodedOffset + stringOffAdd` in
the underlying file. WCX reads preview data from the full file stream, so it
must add `DirectoryStartPosition` explicitly.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```
