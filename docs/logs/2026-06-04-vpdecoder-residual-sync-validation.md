# 2026-06-04 VPDecoder Residual Sync Validation

## Summary

- Updated `external/VPDecoder` from `3dd9681` to `d68d91f`.
- Validated the VP9 inter residual syntax synchronization fix through the WCX
  video export path.

## Validation

- `dotnet build external/VPDecoder/VPDecoder.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test external/VPDecoder/VPDecoder.slnx --no-build -m:1`
- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- Local smoke:
  `Data/Packs/Mob_00002.ms`,
  selector `Mob/BossPattern/BossFirstAdversary.img`,
  value `1069/003/effect/0`,
  exported through `dotnet run --no-build --project src/WzComparerX.Cli -- export --type video`.

## Result

The previous failure:

```text
VP9 full inter residual probe ended unexpectedly at tile 0 MI (104,32); parsed 15 inter mode infos and 45 coefficient groups in this superblock; last inter mode MI (110,38) block Block16X16 transform Tx16X16 skip True ref Last prediction NearMv; last coefficient group block Block16X16 transform Tx8X8 blocks 1.
```

is no longer the active blocker for this sample. The decode now advances to the
next unsupported VP9 inter reconstruction feature:

```text
VP9 fractional-pixel motion compensation is not supported yet.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
