# 2026-06-05 VPDecoder Libvpx-Aligned Validation

## Summary

- Updated `external/VPDecoder` from `bcacbb3` to `3843330`.
- Synced WCX Rendering optional VP9 sample expectations to the pinned
  VPDecoder libvpx-aligned merged BGRA hash.
- Re-ran the full WCX video export smoke for the local GMS
  `BossFirstAdversary.img` sample.

## Validation

- `dotnet build external/VPDecoder/VPDecoder.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test external/VPDecoder/VPDecoder.slnx --no-build -m:1`
- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local smoke:
  - sample: local GMS `Data/Packs/Mob_00002.ms`
  - selector: `Mob/BossPattern/BossFirstAdversary.img`
  - value: `1069/003/effect/0`
  - command: `dotnet run --no-build --project src/WzComparerX.Cli -- export --type video`

## Result

- VPDecoder tests passed: 559 total, 0 failed.
- WCX tests passed:
  - App: 67 total, 0 failed.
  - Core: 143 total, 0 failed.
  - Rendering: 21 total, 0 failed.
  - WzLib: 78 total, 0 failed.
- Local video export wrote `manifest.json` plus 97 BGRA8888 frames.
- The smoke manifest reports `VP90`, `2656x1352`, `97` frames, `Bgra8888`,
  first frame start `0 ns`, and last frame start `5760000000 ns`.

## Notes

- The optional first-frame color+alpha sample expectation was updated to match
  the pinned VPDecoder libvpx-aligned output.
- The GB-scale smoke output remains local-only and is not committed.
