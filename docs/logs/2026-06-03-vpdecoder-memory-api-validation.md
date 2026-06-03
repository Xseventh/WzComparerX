# 2026-06-03 - VPDecoder Memory API Validation

## Context

After WCX validated the initial `external/VPDecoder` submodule, the decoder
repository added explicit memory-first APIs for WCX integration. WCX pulled the
latest submodule commit and re-ran validation against the extracted raw VP9
sample packets.

Submodule commit:

```text
7342b124706175d6ae838ca9158b14830f0f93f5
```

## API Shape

The validated revision exposes:

- `RawVp9Decoder.DecodeFrame(ReadOnlySpan<byte>, Vp9DecodeOptions?)`
- `RawVp9Decoder.DecodeFrame(ReadOnlyMemory<byte>, Vp9DecodeOptions?)`
- `RawVp9Decoder.DecodeFrameWithAlpha(ReadOnlySpan<byte>, ReadOnlySpan<byte>, Vp9DecodeOptions?)`
- `RawVp9Decoder.DecodeFrameWithAlpha(ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, Vp9DecodeOptions?)`
- `RawVp9Decoder.Reset()`

This matches the WCX integration requirement: a future media/rendering adapter
can pass raw VP9 color/alpha frame chunks from memory without temp files,
without WebM/IVF wrapping, and without VPDecoder needing to know WZ/MS/MCV
container details.

## Validation

Commands run:

```bash
dotnet build external/VPDecoder/VPDecoder.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test external/VPDecoder/VPDecoder.slnx --no-build -m:1
dotnet run --no-build --project external/VPDecoder/src/VPDecoder.Cli/VPDecoder.Cli.csproj -- --input /tmp/vp9-main-frame-0.vp9 --alpha /tmp/vp9-alpha-frame-0.vp9 --width 2656 --height 1352 --out /tmp/vp9-merged-frame-0.bgra
```

Results:

- `dotnet build` succeeded with 0 warnings and 0 errors.
- `dotnet test` passed: 401 total, 0 failed, 0 skipped.
- CLI alpha smoke decoded color + alpha to `2656x1352` BGRA8888,
  14,363,648 bytes,
  SHA-256 `c8095ee5e4b760a8a6f7c18d10b357b9f579c6864bb1cd815061d8d6e930a2ff`.

## Notes

- The new API satisfies the memory-level decode requirement for the first WCX
  media adapter slice.
- VPDecoder still keeps broader VP9 inter-frame and VP8 pixel reconstruction as
  explicitly gated follow-up work. WCX should preserve decoder diagnostics
  rather than treating every failure as a generic video failure.
- WCX has not yet added a `WzImageVideoInspection` -> VPDecoder adapter; this
  remains the next implementation step.

