# 2026-04-28 - IMG Property Preview

## Summary

- Extended `preview-img` to read first-layer entries for top-level `Property`
  images.
- Added preview entries for null, integer, floating point, string, and nested
  object property values.
- Kept nested object parsing shallow: `0x09` values report the nested object
  type and skip to the object's end.
- Added deterministic WzLib coverage for simple first-layer `Property` scalar
  values.

## Local Smoke

Verified the local MapleStoryNA `Data/Base/Base_000.wz`:

```bash
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-img path/to/Data/Base/Base_000.wz smap.img
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-img path/to/Data/Base/Base_000.wz StandardPDD.img
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-img path/to/Data/Base/Base_000.wz zmap.img
```

- `smap.img` reports 151 first-layer properties.
- `StandardPDD.img` reports six nested `Property` object entries.
- `zmap.img` reports 155 first-layer properties.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```
