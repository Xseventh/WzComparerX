# 2026-06-04 VPDecoder Clipped Transform Validation

## Summary

- Updated `external/VPDecoder` from `07eea4c` to `2639678`.
- Validated the VP9 clipped inter residual transform slice through the WCX video
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
VP9 inter coefficient block transform offset does not fit the block geometry. (Parameter 'group')
```

is no longer the active blocker for this sample. The decode now advances past
frame 1 and reaches a frame 2 VP9 reference-mode limitation:

```text
VP9 compound or selectable block reference modes are not supported yet.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
