# 2026-04-28 IMG Video Inspection

## Summary

- Added IMG object-value inspection for `Canvas#Video`.
- Captured unknown byte, payload offset, and payload length.
- Reused the mini-property parser shared with RawData inspection.
- Added deterministic tests for video metadata and out-of-bounds payload
  rejection.

## Validation

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- inspect --depth 4 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/UI/UI_000.wz" Basic.img
```

## Notes

- The local GMS smoke file checked in this iteration primarily exercised Canvas
  and Vector paths. Canvas#Video is covered by synthetic fixture bytes.
- Video payload decoding is intentionally not implemented yet.
