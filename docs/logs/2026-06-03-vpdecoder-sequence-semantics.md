# 2026-06-03 - VPDecoder Sequence Semantics

## Context

`external/VPDecoder` published a documentation-only update for raw VP9
cross-frame decode semantics. WCX already has a Rendering adapter for selected
Video frame packet decode, so the next question was whether the VPDecoder API
requires another change before WCX proceeds.

## Changes

- Updated the VPDecoder submodule pointer from `7342b12` to `7e76fde`.
- Recorded the sequence semantics in `docs/video-decode-native-plan.md`.
- Updated `docs/handoff.md` so the next worker knows `DecodeFrameWithAlpha` is
  a single-frame convenience helper, not a full color+alpha sequence decoder.

## Notes

- One `RawVp9Decoder` instance is one VP9 stream state.
- Inter-frame decode requires packets to be fed in order to the same decoder.
- `DecodeFrameWithAlpha` maintains color state on the current decoder but uses
  a fresh internal decoder for alpha during that call.
- Full playback should keep separate color and alpha decoder states, reset both
  together, and merge successfully decoded frames.

## Validation

Verification passed for this pointer update:

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```
