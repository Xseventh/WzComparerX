# 2026-06-05 VPDecoder Compound Sub-8x8 Validation

## Summary

- Updated `external/VPDecoder` from `72f5fc9` to `bcacbb3`.
- Validated the VP9 compound sub-8x8 motion-vector slice through the WCX video
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
Video frame 72 failed with wcx.video.decoder.TruncatedPacket:
VP9 inter partition residual probe ended unexpectedly at tile 4 MI (129,174) block Block8X8.
```

is no longer active. The same sample now completes full sequence export:

```text
/tmp/wcx-boss-first-adversary-video-test-bcacbb3
```

The output directory contains `manifest.json` plus `97` BGRA8888 frame dumps.
Each frame is `2656x1352`, stride `10624`, and `Bgra8888`; the local output is
about `1.3G`. The dump is a local smoke artifact only and is not committed.
