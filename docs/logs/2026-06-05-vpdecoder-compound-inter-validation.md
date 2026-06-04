# 2026-06-05 VPDecoder Compound Inter Validation

## Summary

- Updated `external/VPDecoder` from `2639678` to `66cd5cb`.
- Validated the VP9 compound inter sequence decoding slice through the WCX video
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
VP9 compound or selectable block reference modes are not supported yet.
```

is no longer the active blocker for this sample. The decode now advances past
frame 2 and reaches frame 41 before hitting the next unsupported VP9
reconstruction feature:

```text
VP9 inter residual reconstruction for clipped transform blocks is not supported yet.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
