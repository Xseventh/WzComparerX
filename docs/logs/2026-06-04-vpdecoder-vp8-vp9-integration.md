# 2026-06-04 VPDecoder VP8/VP9 Integration

## Summary

- Updated `external/VPDecoder` from `7e76fde` to `0a6c7ee`.
- Integrated the newer memory-first decoder surface into
  `WzComparerX.Rendering`.
- Kept App and CLI video preview/export on the shared Rendering path; no parser
  behavior was added to App view models.

## Code Changes

- Added a `VP80` raw packet adapter alongside the existing `VP90` adapter.
- Treated successful no-display raw decoder results as distinct from failed
  packets.
- Updated sequence decode to feed VP9 no-display packets into decoder state and
  skip them in exported/previewed display frames.
- Updated selected-frame decode to report a stable
  `wcx.video.frame.noDisplay` diagnostic when the requested packet is a
  no-display frame.
- Kept codec selection behind a `fourCC`-aware decoder factory so future codec
  backends can be swapped without changing CLI or App workflow code.

## Validation

- `dotnet build external/VPDecoder/VPDecoder.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test external/VPDecoder/VPDecoder.slnx --no-build -m:1`
- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.Rendering.Tests/WzComparerX.Rendering.Tests.csproj --no-build -m:1`
- `dotnet test WzComparerX.slnx --no-build -m:1`

All validation passed before committing this iteration.

## Notes

Real GMS video samples remain useful smoke targets, but representative clips can
write hundreds of MB to GB of BGRA frame dumps. This iteration therefore treats
full real-client video export as a dedicated follow-up smoke slice rather than a
routine verification step.

The pinned VPDecoder revision still documents VP8 as a gated key-frame subset;
broader VP8 inter/reference behavior should continue to surface explicit
decoder diagnostics until VPDecoder grows that coverage.
