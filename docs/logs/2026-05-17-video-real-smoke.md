# 2026-05-17 Video Real Smoke

## Context

Milestone 5 still had a real-client coverage gap for `Canvas#Video`: synthetic
fixtures covered metadata inspection and diagnostics, but the local GMS sample
had not been recorded with a direct video value.

## Changes

- Added an optional external-client Core smoke test for:
  - package: `Data/UI/UI_000.wz`
  - selector: `UIGachapon.img`
  - value path: `royalStyle/openvideo/intro`
- The smoke locks the observed video metadata:
  - `unknown = 1`
  - `dataLength = 1943143`
  - `dataOffset = 27488749`
- The same test verifies the stable `wcx.payload.video.unsupported` info
  diagnostic, since video payload decoding/playback remains deferred to the
  media milestone.
- Updated the parser coverage matrix and format notes so Sound, RawData, and
  Video all have at least one real-client metadata smoke path.

## Notes

- The sample was found with parser-based inspection, not plaintext scanning.
- No client files were added to the repository.
