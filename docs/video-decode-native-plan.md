# Video Decode Plan

This note defines the dependency shape for future Canvas#Video frame decode.
WCX already parses `Canvas#Video` `MCV0` metadata in WzLib; actual VP8/VP9
decode belongs in a later media/rendering layer, not in WzLib.

As of 2026-06-03, WCX tracks a managed raw packet decoder experiment as a
submodule:

```text
external/VPDecoder
```

The submodule is the preferred first integration candidate for current VP9
samples because it avoids cross-platform native asset work for the initial
decode slice. The native `libvpx` / `libyuv` matrix below remains the fallback
or comparison path if the managed decoder cannot cover a required bitstream
shape.

## WC Reference

WC uses:

- `WzComparerR2.WzLib/Wz_Video.cs` for `MCV0` header and frame table parsing.
- `WzComparerR2.Common/VpxVideoDecoder.cs` for `libvpx` interop.
- `WzComparerR2.Common/Animation/MaplestoryCanvasVideoLoader.cs` for frame
  decode, alpha-map merge, and I420-to-BGRA conversion through `libyuv`.

WC ships Windows native libraries under:

```text
WzComparerR2/References/x86/libvpx.dll
WzComparerR2/References/x86/libyuv.dll
WzComparerR2/References/x64/libvpx.dll
WzComparerR2/References/x64/libyuv.dll
WzComparerR2/References/ARM64/libvpx.dll
WzComparerR2/References/ARM64/libyuv.dll
```

Those DLLs are Windows PE binaries only. They are suitable as the initial
Windows source for WCX native assets, but they do not cover macOS or Linux.

## Target RID Matrix

Use NuGet-style runtime native asset layout when the media project lands:

```text
runtimes/win-x86/native/libvpx.dll
runtimes/win-x86/native/libyuv.dll
runtimes/win-x64/native/libvpx.dll
runtimes/win-x64/native/libyuv.dll
runtimes/win-arm64/native/libvpx.dll
runtimes/win-arm64/native/libyuv.dll

runtimes/osx-x64/native/libvpx.dylib
runtimes/osx-x64/native/libyuv.dylib
runtimes/osx-arm64/native/libvpx.dylib
runtimes/osx-arm64/native/libyuv.dylib

runtimes/linux-x64/native/libvpx.so
runtimes/linux-x64/native/libyuv.so
runtimes/linux-arm64/native/libvpx.so
runtimes/linux-arm64/native/libyuv.so
```

Alpine/musl support is optional and should be added only if WCX explicitly
commits to musl builds:

```text
runtimes/linux-musl-x64/native/libvpx.so
runtimes/linux-musl-x64/native/libyuv.so
runtimes/linux-musl-arm64/native/libvpx.so
runtimes/linux-musl-arm64/native/libyuv.so
```

## Layering

- `WzComparerX.WzLib`: parses `MCV0` header, frame tables, and exposes frame
  chunk offsets/counts. It must not load native libraries.
- `WzComparerX.Core`: selects resource values and reports diagnostics.
- Future `WzComparerX.Media` or `WzComparerX.Rendering`: owns VPx decode,
  I420-to-BGRA conversion, alpha-map merge, frame timing, and optional playback
  surfaces. This layer may consume the managed `external/VPDecoder` submodule or
  a native backend behind the same media-facing interface.
- `WzComparerX.App` and CLI consume decoded frame documents through Core/media
  services, not through direct parser access.

## First Implementation Slice

1. Add a media-layer decoder interface that accepts `WzImageVideoInspection`
   plus an image stream accessor and returns BGRA8888 frames. Implemented in
   `WzComparerX.Rendering` as `WzImageVideoFrameDecoder`.
2. Adapt `external/VPDecoder` as the first backend for raw `VP90` frame packets,
   including alpha-map merge for samples that carry alpha frame chunks.
   Implemented as `Vp9RawVideoPacketDecoder`.
3. Keep native `libvpx.dll` / `libyuv.dll` assets as a comparison/fallback
   backend, starting with WC's Windows `References` folders if the native path
   becomes necessary.
4. Add macOS and Linux native libraries from reproducible builds or trusted
   package artifacts before claiming cross-platform native video decode.
5. Add optional runtime diagnostics for missing or unsupported decoder backends
   rather than failing general inspection.
6. Validate against the local GMS sample
   `Packs/Mob_00002.ms` / `Mob/BossPattern/BossFirstAdversary.img` /
   `1069/003/effect/0`, which uses `VP90` and alpha-map data.

VP9 is required for current GMS samples. VP8 should be supported through the
same media abstraction because older or different clients may still use `VP80`.

## Managed Decoder Validation

`external/VPDecoder` was added and validated against the extracted first
`VP90` color and alpha frame packets from the local GMS sample above. It is
currently pinned to:

```text
7342b124706175d6ae838ca9158b14830f0f93f5
```

This revision exposes memory-first library APIs for WCX-style integration:

- `DecodeFrame(ReadOnlySpan<byte>, ...)`
- `DecodeFrame(ReadOnlyMemory<byte>, ...)`
- `DecodeFrameWithAlpha(ReadOnlySpan<byte>, ReadOnlySpan<byte>, ...)`
- `DecodeFrameWithAlpha(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, ...)`
- `Reset()`

Validated commands:

```bash
dotnet restore external/VPDecoder/VPDecoder.slnx
dotnet build external/VPDecoder/VPDecoder.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test external/VPDecoder/VPDecoder.slnx --no-build -m:1
dotnet run --no-build --project external/VPDecoder/src/VPDecoder.Cli/VPDecoder.Cli.csproj -- --input /tmp/vp9-main-frame-0.vp9 --width 2656 --height 1352 --out /tmp/vp9-main-frame-0.bgra
dotnet run --no-build --project external/VPDecoder/src/VPDecoder.Cli/VPDecoder.Cli.csproj -- --input /tmp/vp9-alpha-frame-0.vp9 --width 2656 --height 1352 --out /tmp/vp9-alpha-frame-0.bgra
dotnet run --no-build --project external/VPDecoder/src/VPDecoder.Cli/VPDecoder.Cli.csproj -- --input /tmp/vp9-main-frame-0.vp9 --alpha /tmp/vp9-alpha-frame-0.vp9 --width 2656 --height 1352 --out /tmp/vp9-merged-frame-0.bgra
```

Observed results:

- VPDecoder tests passed: 401 total, 0 failed.
- Main frame decoded to `2656x1352` BGRA8888, 14,363,648 bytes,
  SHA-256 `bd018f0c6eac5ae58945a2517c96c29a40f703b6c8c0a07c99debb9a8a864902`.
- Alpha frame decoded to `2656x1352` BGRA8888, 14,363,648 bytes,
  SHA-256 `de5f6cf32681237d0076b8e106c2d8803a54379f639d9f6e7d10a864ad1ff306`.
- Color + alpha decoded to `2656x1352` BGRA8888, 14,363,648 bytes,
  SHA-256 `c8095ee5e4b760a8a6f7c18d10b357b9f579c6864bb1cd815061d8d6e930a2ff`.
- Library tests cover `ReadOnlyMemory<byte>` input and alpha composition
  through `DecodeFrameWithAlpha`.
- The VPDecoder CLI also exposes `--alpha` for smoke validation, but WCX should
  integrate the library API directly rather than shelling out to the CLI.

## WCX Adapter Validation

WCX now has a Rendering-layer adapter:

```text
WzComparerX.Rendering/WzImageVideoFrameDecoder
```

The adapter accepts an image payload stream plus `WzImageVideoInspection`, reads
selected color/alpha frame chunks from memory, and delegates raw `VP90` packet
decode to `Vp9RawVideoPacketDecoder`.

This keeps WzLib limited to `MCV0` metadata/frame-table parsing. Core/App/CLI
have not been wired to video decode yet, so resource inspection still reports
Video payload decode as unsupported until a media-facing Core service is added.

## VP9 Sequence Semantics

The pinned VPDecoder revision documents cross-frame behavior:

- one `RawVp9Decoder` instance represents one VP9 stream state;
- packets must be fed to that instance in display/decode order when inter-frame
  references are required;
- `Reset()` or a fresh decoder is required when switching streams, seeking to a
  point without references, or replaying from the beginning;
- returned pixel buffers are caller-owned and do not mutate decoder reference
  slots;
- `DecodeFrameWithAlpha` is a single-frame convenience API. It maintains color
  state on the current decoder, but decodes alpha with a fresh internal decoder
  for that call.

That means the current `WzImageVideoFrameDecoder` is suitable for selected-frame
or first-frame preview when packets are independently decodable. A future
playback or full color+alpha sequence service should keep two decoder states:
one for color and one for alpha, reset both together, decode both packet streams
in order, and then merge the decoded frames.
