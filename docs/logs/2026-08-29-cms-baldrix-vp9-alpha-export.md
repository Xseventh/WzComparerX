# 2026-08-29 CMS Baldrix VP9 Alpha Export

## Summary

- Reproduced a WCX CLI crash while exporting CMS v227.7 `BossBaldrix.img`
  VP90 + AlphaMap videos at `1710x1040`.
- Updated the `external/VPDecoder` submodule from `3843330` to `4cb3719`.
- Confirmed the coded-plane padding fix prevents the chroma 16-pixel vertical
  loop filter from indexing past the reconstructed plane at the coded right
  edge.

## Coverage

- Container: local CMS v227.7 `Data/Packs/Mob_00000.ms`.
- Selector: `Mob/BossPattern/BossBaldrix.img`.
- Values:
  - `1045/002/screen/video`: 45 frames.
  - `1045/002/screen2/video`: 41 frames.
  - `1050/003/screen/video`: packet-identical to the first sequence.
  - `1050/003/screen2/video`: packet-identical to the second sequence.
- All raw client packets, temporary IVF files, reference YUV, and exported
  BGRA frames stayed under `/tmp` and are not repository fixtures.

## Validation

- Built VPDecoder and decoded the two distinct color + alpha packet sequences
  with persistent per-stream decoder state: 172 successful raw frame decodes.
- Decoded the same temporary IVF streams through libvpx 1.16.0 and compared
  visible Y, U, and V bytes:
  - 45-frame color SHA-256: `d3df3cda1a577cace446b8526c513a678baa41f04faf1213803944d1936bc90f`.
  - 45-frame alpha SHA-256: `575dbbbe7f6a3f362f5d37f5cb77a95320c5933ab6f7c6cc59bb896be135837e`.
  - 41-frame color SHA-256: `f808836db466b570fedb8a7bafdf7c744f5cf6cc5cf886dedca92cab5f0790b9`.
  - 41-frame alpha SHA-256: `90de96b72927fb9c575f36d468b19c648611878c485fc6a836d8848ec9d33da2`.
  - All four managed/libvpx comparisons were byte-identical.
- Rebuilt WCX and ran `export --type video` for all four listed resource
  values. Each command exited successfully; the CLI wrote 45 or 41 BGRA8888
  frames plus manifests with stride 6840 and 60 ms frame delays. First-frame
  alpha values span 0 through 255, confirming AlphaMap merge rather than opaque
  fallback.
- `dotnet test WzComparerX.slnx --no-build -m:1`: 317 passed, 0 failed.
- VPDecoder coded-padding/reconstruction/motion focused tests: 31 passed,
  0 failed. The complete VPDecoder run had 511 passing tests and 58 expected
  local failures because the older `/tmp/vp9-main-frame-0.vp9` and
  `/tmp/vp9-alpha-frame-0.vp9` acceptance fixtures were not present.

## Result

- The former unhandled `IndexOutOfRangeException` in
  `Vp9LoopFilter.ApplyVertical16` no longer occurs.
- Non-MI-aligned visible dimensions retain coded padding during reconstruction
  and filtering while public output planes remain `1710x1040`.
