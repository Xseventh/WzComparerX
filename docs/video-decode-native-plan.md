# Video Decode Native Library Plan

This note defines the native dependency shape for future Canvas#Video frame
decode. WCX already parses `Canvas#Video` `MCV0` metadata in WzLib; actual
VP8/VP9 decode belongs in a later media/rendering layer, not in WzLib.

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
- Future `WzComparerX.Media` or `WzComparerX.Rendering`: owns native VPx decode,
  I420-to-BGRA conversion, alpha-map merge, frame timing, and optional playback
  surfaces.
- `WzComparerX.App` and CLI consume decoded frame documents through Core/media
  services, not through direct parser access.

## First Implementation Slice

1. Add a media-layer decoder interface that accepts `WzImageVideoInspection`
   plus an image stream accessor and returns BGRA8888 frames.
2. Source Windows `libvpx.dll` / `libyuv.dll` from WC's `References` folders
   into the target RID layout.
3. Add macOS and Linux native libraries from reproducible builds or trusted
   package artifacts before claiming cross-platform video decode.
4. Add optional runtime diagnostics for missing native dependencies rather than
   failing general inspection.
5. Validate against the local GMS sample
   `Packs/Mob_00002.ms` / `Mob/BossPattern/BossFirstAdversary.img` /
   `1069/003/effect/0`, which uses `VP90` and alpha-map data.

VP9 is required for current GMS samples. VP8 should be supported through the
same libvpx path because older or different clients may still use `VP80`.
