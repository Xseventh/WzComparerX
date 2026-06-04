# 2026-06-04 VPDecoder Mixed Sub-8x8 Validation

## Summary

- Updated `external/VPDecoder` from `f0403f6` to `64c850c`.
- Validated the VP9 mixed sub-8x8 inter mode slice through the WCX video export
  path.

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
VP9 sub-8x8 inter blocks with mixed sub-block prediction modes are not supported yet.
```

is no longer the active blocker for this sample. The decode now advances to the
next unsupported VP9 inter feature:

```text
VP9 sub-8x8 NEWMV inter prediction mode is not supported yet.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
