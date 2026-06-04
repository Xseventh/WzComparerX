# 2026-06-05 VPDecoder Skip Context Validation

## Summary

- Updated `external/VPDecoder` from `059d5d5` to `2fce6c3`.
- Validated the VP9 residual skip-context alignment slice through the WCX video export path.

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
VP9 inter partition residual probe ended unexpectedly at tile 0 MI (140,30) block Block16X16.
```

is no longer the active exact failure. The decode still fails on frame 51, but
the residual sync drift moved to a different block:

```text
VP9 inter partition residual probe ended unexpectedly at tile 0 MI (138,25) block Block8X8.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
