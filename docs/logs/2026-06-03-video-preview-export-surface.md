# 2026-06-03 - Video Preview And Export Surface

## Context

WC treats `Canvas#Video` as a full frame-table animation sequence, not as a
single selected frame. WCX already had `MCV0` metadata inspection and a
Rendering sequence decoder, but App/CLI still needed a user-facing path into
that decoder.

## Changes

- Added Core video target selection for root Video IMG entries and selected
  `Canvas#Video` property paths.
- Added Rendering `ResourceVideoSequenceService` and a sequence document model
  that wraps `WzImageVideoSequenceDecoder`.
- Added CLI `export --type video --out <directory> --value <property-path>`,
  writing `manifest.json` plus BGRA8888 `frame-0000.bgra` files when decode
  succeeds.
- Added Avalonia video preview workflow, bitmap factory, and preview view model
  so IMG Content video nodes can drive the Preview tab.
- Shared the existing Preview scale controls through a bitmap-preview base view
  model, so Canvas and Video previews use the same display-scale behavior.
- Wrapped frame decode failures with `wcx.video.frame.decodeFailed` so App/CLI
  diagnostics include the failing frame index and the inner decoder code.

## Tests

- Core tests cover video target selection and CLI video export diagnostics.
- Rendering tests cover frame-level decode failure diagnostics.
- App tests cover the video preview view model and the Resources -> IMG Content
  -> Video Preview ViewModel path with a synthetic `MCV0` fixture and a fake
  raw video decoder.

## Real-Client Smoke

The video surfaces now reach real local GMS video packets, but full real-client
decode is still blocked by VPDecoder coverage:

- `Data/UI/UI_000.wz`, selector `UIGachapon.img`, value
  `royalStyle/openvideo/intro` fails at frame 0 with
  `wcx.video.frame.decodeFailed` wrapping VPDecoder `InternalDecodeFailure`:
  coefficient block count does not match the block geometry.
- `Data/Packs/Mob_00002.ms`, selector `Mob/BossPattern/BossFirstAdversary.img`,
  value `1069/003/effect/0` fails at frame 1 with
  `wcx.video.frame.decodeFailed` wrapping VPDecoder
  `UnsupportedInterFrameFeature`: non-display/reference state is not supported
  yet.

This confirms the WZ/MS path, image selection, video metadata, packet slicing,
and App/CLI entry points are wired. WC-compatible video playback/export still
requires broader VPDecoder VP9 inter-frame coverage, VP8 pixel reconstruction,
or a native libvpx backend behind the same Rendering interface.
