# 2026-06-01 - Video Native Dependency Plan

## Context

After adding Canvas#Video `MCV0` metadata inspection, the next question was how
to support actual VP8/VP9 frame decode across Windows, macOS, and Linux.

## Decision

- Do not put native decoder loading in WzLib.
- Future video decode belongs in a media/rendering layer that consumes WzLib
  video metadata and frame chunk offsets.
- Windows native libraries can initially be sourced from WC's checked-in
  `References/x86`, `References/x64`, and `References/ARM64` folders.
- macOS and Linux still need matching `libvpx` and `libyuv` builds before WCX
  can claim cross-platform video decode.

## Notes

The target native asset matrix is documented in
`docs/video-decode-native-plan.md`. Current GMS samples already require VP9:
`Packs/Mob_00002.ms` / `BossFirstAdversary.img` reports `fourCc = VP90`.
