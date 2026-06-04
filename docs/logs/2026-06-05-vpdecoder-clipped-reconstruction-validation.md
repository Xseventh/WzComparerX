# 2026-06-05 VPDecoder Clipped Reconstruction Validation

## Summary

- Updated `external/VPDecoder` from `66cd5cb` to `059d5d5`.
- Validated the VP9 clipped inter residual reconstruction slice through the WCX
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
VP9 inter residual reconstruction for clipped transform blocks is not supported yet.
```

is no longer the active blocker for this sample. The decode now advances past
frame 41 and reaches frame 51 before hitting a VP9 partition residual
synchronization diagnostic:

```text
VP9 inter partition residual probe ended unexpectedly at tile 0 MI (140,30) block Block16X16.
```

No video frame dump was written because decode fails before the CLI creates the
output directory.
