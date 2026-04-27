# 2026-04-28 IMG Sound Preview

## Summary

- Added IMG object-value preview for `Sound_DX8`.
- Captured version, duration, sound declaration, AM media type GUIDs, fixed-size
  and temporal-compression flags, optional format-extra length, payload offset,
  and payload length.
- Preserved WC's version-1 mini-property behavior and `soundDecl == 2`
  format-extra skip behavior.
- Added deterministic tests for sound metadata and out-of-bounds payload
  rejection.

## Validation

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-img --depth 2 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Sound/Sound_000.wz" AchievementEff.img
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-img --depth 2 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Sound/Sound_000.wz" Bgm00.img
```

## Notes

- Local GMS smoke confirms `AchievementEff.img` and `Bgm00.img` now expose
  `Sound_DX8` metadata.
- Audio payload decoding and WAVEFORMATEX decryption/parsing remain later work.
