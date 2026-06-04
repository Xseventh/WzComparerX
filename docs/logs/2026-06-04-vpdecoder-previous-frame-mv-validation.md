# 2026-06-04 VPDecoder Previous-Frame MV Validation

## Summary

- Updated `external/VPDecoder` from `c0a3cd4` to `a82f614`.
- Validated the VP9 previous-frame MV candidate slice through the WCX video
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
VP9 NEWMV requires a spatial or previous-frame MV candidate; previous-frame MV fallback is not supported yet.
```

is no longer the active blocker for this sample. The decode still fails in the
NEWMV candidate path, but now with the narrower diagnostic:

```text
VP9 NEWMV requires a spatial or eligible previous-frame MV candidate.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
