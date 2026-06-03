# 2026-06-03 - Video Sequence Decoder

## Context

WC handles `Canvas#Video` as an animation sequence: selecting a `Wz_Video`
node loads the full `MCV0` frame table, decodes every frame in order, and
returns `FrameAnimationData`. When alpha-map data is present, WC maintains a
separate VPX decoder for the alpha stream and merges alpha into BGRA output.

WCX already had a selected-frame Rendering adapter. That was useful for
diagnostics, but not the right primary behavior for video playback.

## Changes

- Added `WzImageVideoSequenceDecoder` in `WzComparerX.Rendering`.
- Added `WzDecodedVideoSequence` and `WzVideoSequenceDecodeResult`.
- Added shared video decode primitives for input validation and packet reads.
- Added an internal frame composer that mirrors WC alpha behavior by copying
  the alpha frame red channel into the color frame alpha channel.
- Kept `WzImageVideoFrameDecoder` as an auxiliary selected-frame decoder, but
  moved shared validation and packet slicing out of it.

## Tests

Rendering tests now cover:

- full frame-table decode order;
- separate color and alpha decoder state creation;
- alpha red-channel merge into BGRA and optional RGBA output conversion;
- no-alpha sequences using only one decoder state;
- unsupported codec diagnostics;
- color decode failure stopping before alpha decode;
- alpha dimension mismatch diagnostics;
- invalid output option diagnostics;
- optional local `/tmp` VP9 color+alpha sample smoke through the real backend.

## Notes

- Core/App/CLI are not wired to video decode yet.
- The next user-visible slice should connect App Preview to
  `WzImageVideoSequenceDecoder` and expose play/pause/step behavior.
- CLI export can use the same sequence decoder later for frame dumps or an
  explicit video export contract.
