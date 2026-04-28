# 2026-04-28 IMG Property Depth

## Summary

- Added bounded recursive IMG `Property` inspection support.
- Added `inspect --depth n`; the default remains first-layer inspection.
- Kept `--depth 0` available for object-type-only smoke checks.
- Bounded accepted inspection depth to `0` through `64`.
- Added deterministic nested-property reader coverage.

## Validation

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect --depth 2 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Base/Base_000.wz" StandardPDD.img
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect --depth 0 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Base/Base_000.wz" smap.img
```

## Notes

- Nested object parsing is intentionally bounded by caller-provided depth.
- Nested object values that are not `Property` are still summarized by object
  type.
