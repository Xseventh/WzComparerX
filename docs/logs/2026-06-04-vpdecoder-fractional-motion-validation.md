# 2026-06-04 VPDecoder Fractional Motion Validation

## Summary

- Updated `external/VPDecoder` from `d68d91f` to `07eea4c`.
- Validated the VP9 fractional-pixel motion compensation slice through the WCX
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
VP9 fractional-pixel motion compensation is not supported yet.
```

is no longer the active blocker for this sample. The decode now advances to a
VP9 coefficient transform geometry diagnostic:

```text
VP9 inter coefficient block transform offset does not fit the block geometry. (Parameter 'group')
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
