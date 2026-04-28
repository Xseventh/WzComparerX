# 2026-04-28 - IMG Object Type Inspection

## Summary

- Added `WzImageInspectionReader` to read an IMG payload's top-level object type
  from a calculated PKG1 image offset.
- Added Core service and text/JSON formatters for image inspection.
- Added CLI command:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- inspect path/to/file.wz smap.img
```

- Added deterministic tests for inline and referenced IMG object type strings.

## Local Smoke

Verified the local MapleStoryNA `Data/Base/Base_000.wz`:

```bash
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect path/to/Data/Base/Base_000.wz smap.img
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect --json path/to/Data/Base/Base_000.wz zmap.img
```

Both selected images report top-level object type `Property`.

## Notes

This is intentionally not full IMG parsing yet. It only proves that directory
inspection offsets can enter an image payload and read the first object type using
WC-compatible image string rules.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```
