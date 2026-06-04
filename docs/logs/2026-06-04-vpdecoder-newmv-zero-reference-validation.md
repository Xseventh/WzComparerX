# 2026-06-04 VPDecoder NEWMV Zero Reference Validation

## Summary

- Updated `external/VPDecoder` from `a82f614` to `f0403f6`.
- Validated the VP9 NEWMV zero-reference fallback slice through the WCX video
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
VP9 NEWMV requires a spatial or eligible previous-frame MV candidate.
```

is no longer the active blocker for this sample. The decode now advances to the
next unsupported VP9 inter feature:

```text
VP9 sub-8x8 inter blocks with mixed sub-block prediction modes are not supported yet.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
