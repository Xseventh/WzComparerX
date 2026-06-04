# 2026-06-04 VPDecoder Sub-8x8 NEWMV Validation

## Summary

- Updated `external/VPDecoder` from `64c850c` to `3dd9681`.
- Validated the VP9 sub-8x8 NEWMV / non-zero NEARMV slices through the WCX
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
VP9 sub-8x8 NEWMV inter prediction mode is not supported yet.
```

is no longer the active blocker for this sample. The decode now advances to a
VP9 residual/tile synchronization diagnostic:

```text
VP9 full inter residual probe ended unexpectedly at tile 0 MI (104,32); parsed 15 inter mode infos and 45 coefficient groups in this superblock; last inter mode MI (110,38) block Block16X16 transform Tx16X16 skip True ref Last prediction NearMv; last coefficient group block Block16X16 transform Tx8X8 blocks 1.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
