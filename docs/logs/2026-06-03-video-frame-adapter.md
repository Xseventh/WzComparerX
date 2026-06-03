# 2026-06-03 - Video Frame Decode Adapter

## Context

WCX had validated `external/VPDecoder` as a memory-first raw `VP90` decoder,
but the decoder was still only a submodule. The next M5 slice was to add the
WCX-side adapter without moving media decode into WzLib.

## Changes

- Added `WzComparerX.Rendering` video decode models for packed video frames,
  options, diagnostics, and raw packet decoder results.
- Added `Vp9RawVideoPacketDecoder`, a Rendering-layer wrapper over
  `VPDecoder.RawVp9Decoder`.
- Added `WzImageVideoFrameDecoder`, which accepts an image payload stream plus
  `WzImageVideoInspection`, reads the selected color/alpha frame chunks from
  memory, and decodes them through the raw packet decoder.
- Added `WzComparerX.Rendering.Tests` to keep media adapter tests out of
  Core/WzLib/App tests.

## Validation

The new adapter tests cover:

- color packet slicing from an image payload stream;
- optional alpha packet slicing;
- unsupported codec diagnostics;
- invalid frame index diagnostics;
- packet bounds diagnostics;
- missing MCV header diagnostics;
- decoder `Reset()` forwarding;
- invalid output option diagnostics;
- optional local `/tmp` VP9 color+alpha sample smoke through the real
  VPDecoder backend.

Commands run:

```bash
dotnet restore WzComparerX.slnx
dotnet test tests/WzComparerX.Rendering.Tests/WzComparerX.Rendering.Tests.csproj -m:1
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

## Notes

- WzLib still only parses `MCV0` metadata and frame tables.
- Rendering now owns the raw video decode adapter boundary.
- Core/App/CLI are not wired to video decode yet; current inspect behavior still
  reports Video payload decode as unsupported until a media-facing Core service
  is added.

