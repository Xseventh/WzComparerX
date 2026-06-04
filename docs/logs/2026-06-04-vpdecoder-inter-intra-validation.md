# 2026-06-04 VPDecoder Inter-Frame Intra Block Validation

## Summary

- Updated `external/VPDecoder` from `d6dfa20` to `90a5b84`.
- Validated the VP9 intra-blocks-inside-inter-frames slice through the WCX video
  export path.

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
VP9 intra blocks inside ordinary inter frames are not supported yet.
```

is no longer the active blocker for this sample. The decode now advances to the
next unsupported VP9 inter feature:

```text
VP9 NEWMV requires a same-reference spatial MV candidate; different-reference and previous-frame MV fallback are not supported yet.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
