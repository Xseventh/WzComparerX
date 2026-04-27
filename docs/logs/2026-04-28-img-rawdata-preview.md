# 2026-04-28 IMG RawData Preview

## Summary

- Added IMG object-value preview for `RawData`.
- Captured RawData version, payload offset, and payload length.
- Preserved WC's version-1 mini-property read behavior so streams stay aligned.
- Added deterministic tests for RawData metadata and out-of-bounds payload
  rejection.

## Validation

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-img --depth 4 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/UI/UI_000.wz" Basic.img
```

## Notes

- The local GMS smoke files checked in this iteration primarily exercised
  Canvas and Vector paths. RawData is covered by synthetic fixture bytes.
- RawData payload decoding is intentionally not implemented yet.
