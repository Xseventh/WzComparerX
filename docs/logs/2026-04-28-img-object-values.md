# 2026-04-28 IMG Object Values

## Summary

- Added IMG object-value inspection for `Shape2D#Vector2D`.
- Added IMG object-value inspection for `UOL` paths.
- Added Canvas metadata inspection without decoding pixel payloads.
- Added deterministic synthetic tests for Vector, UOL, and Canvas metadata.

## Validation

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect --depth 2 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/UI/UI_000.wz" Basic.img
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect --depth 3 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/UI/UI_000.wz" Basic.img
```

## Notes

- Canvas inspections report dimensions, texture format, scale, pages, payload
  offset, and payload length.
- Canvas child properties are parsed so metadata can be reached even when the
  caller does not request a deep inspection.
- Canvas PNG/pixel decoding remains a later milestone.
