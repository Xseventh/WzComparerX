# 2026-05-30 Canvas Bitmap Decoder Boundary

## Context

Milestone 5 expanded Canvas preview coverage across multiple WC-supported
direct-zlib bitmap formats, but the capability lived in the Core preview
service. CLI `export --type canvas` still used a narrower export path, which
made export weaker than preview and left pixel-format knowledge above WzLib.

## Changes

- Moved Canvas bitmap conversion into WzLib as a shared BGRA8888 decoder.
- Kept WzLib responsible for Canvas payload bytes, pixel-format conversion, and
  direct-zlib bitmap decode failures.
- Updated Core preview and Core export services to call the same WzLib decoder.
- Updated CLI Canvas export so supported direct-zlib Canvas formats write
  canonical BGRA8888 bytes instead of the older narrow raw-byte subset.
- Mapped WzLib decode failures back into Core diagnostics for preview and
  export-specific messages.
- Removed the previous preview-only BC7 decoder from Core.
- Converted the BC7 SIMD load/store path to safe span-based intrinsics usage,
  so WzLib no longer requires `AllowUnsafeBlocks`.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.WzLib.Tests/WzComparerX.WzLib.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~WzImageCanvasPayloadDecoderTests`
- `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter "FullyQualifiedName~CanvasImageService_Converts|FullyQualifiedName~ExportCanvas"`
- `dotnet test WzComparerX.slnx --no-build -m:1`

## Notes

- WzLib owns format facts and byte-to-pixel conversion.
- Core still owns resource selection, link resolution, export shape, and user
  diagnostics.
- App remains a consumer of Core preview state and does not gain parser logic.
