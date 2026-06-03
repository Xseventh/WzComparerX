# 2026-06-03 - VPDecoder Submodule Validation

## Context

The raw VP9 decoder spike was completed in a separate repository. WCX added it
as a submodule so future Canvas#Video decode work can evaluate a managed decoder
backend before committing to a native `libvpx` / `libyuv` dependency matrix.

Submodule:

```text
external/VPDecoder
```

Pinned commit:

```text
74727264e3c54034362a685572d0d70799ad011a
```

## Validation

Commands run:

```bash
dotnet restore external/VPDecoder/VPDecoder.slnx
dotnet build external/VPDecoder/VPDecoder.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test external/VPDecoder/VPDecoder.slnx --no-build -m:1
dotnet run --no-build --project external/VPDecoder/src/VPDecoder.Cli/VPDecoder.Cli.csproj -- --input /tmp/vp9-main-frame-0.vp9 --width 2656 --height 1352 --out /tmp/vp9-main-frame-0.bgra
dotnet run --no-build --project external/VPDecoder/src/VPDecoder.Cli/VPDecoder.Cli.csproj -- --input /tmp/vp9-alpha-frame-0.vp9 --width 2656 --height 1352 --out /tmp/vp9-alpha-frame-0.bgra
```

Results:

- `dotnet restore` succeeded when run with normal network/process permissions;
  the first sandboxed restore attempt stalled without output and was killed.
- `dotnet build` succeeded with 0 warnings and 0 errors.
- `dotnet test` passed: 399 total, 0 failed, 0 skipped.
- Main raw VP9 packet decoded to `2656x1352` BGRA8888,
  14,363,648 bytes,
  SHA-256 `bd018f0c6eac5ae58945a2517c96c29a40f703b6c8c0a07c99debb9a8a864902`.
- Alpha raw VP9 packet decoded to `2656x1352` BGRA8888,
  14,363,648 bytes,
  SHA-256 `de5f6cf32681237d0076b8e106c2d8803a54379f639d9f6e7d10a864ad1ff306`.
- The main BGRA output was converted to PNG with ImageMagick and visually
  confirmed as nonblank effect imagery.

## Notes

- VPDecoder's core API is container-agnostic and takes raw VP9 packets, which
  matches WCX's intended layering: WzLib parses `MCV0` metadata/frame chunks,
  while a future media/rendering layer owns frame decode and alpha composition.
- `RawVp9Decoder.DecodeFrameWithAlpha` is covered by tests. The VPDecoder CLI
  currently decodes one packet at a time and does not expose an `--alpha`
  composition option.
- WCX has not integrated the decoder into Core/App yet. The next slice should
  add a media-facing adapter that consumes `WzImageVideoInspection` frame
  metadata and calls VPDecoder for `VP90` chunks.

