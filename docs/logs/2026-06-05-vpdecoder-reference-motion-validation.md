# 2026-06-05 VPDecoder Reference Motion Validation

## Summary

- Updated `external/VPDecoder` from `2fce6c3` to `72f5fc9`.
- Validated the VP9 reference motion-vector clamp slice through the WCX video
  export path.

## Validation

- `dotnet build external/VPDecoder/VPDecoder.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test external/VPDecoder/VPDecoder.slnx --no-build -m:1`
- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- Local smoke:
  - sample: local GMS `Data/Packs/Mob_00002.ms`
  - selector: `Mob/BossPattern/BossFirstAdversary.img`
  - value: `1069/003/effect/0`
  - command: `dotnet run --no-build --project src/WzComparerX.Cli -- export --type video`

## Result

The previous failure:

```text
Video frame 51 failed with wcx.video.decoder.TruncatedPacket:
VP9 inter partition residual probe ended unexpectedly at tile 0 MI (138,25) block Block8X8.
```

is no longer the active failure. The decode now reaches frame 72 before failing:

```text
Video frame 72 failed with wcx.video.decoder.TruncatedPacket:
VP9 inter partition residual probe ended unexpectedly at tile 4 MI (129,174) block Block8X8.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
