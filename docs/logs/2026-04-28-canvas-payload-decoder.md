# 2026-04-28 - Canvas Payload Decoder

## Summary

- Added the first WzLib Canvas payload decoder slice.
- Added `WzImageCanvasBitmap` for decoded Canvas pixel data.
- Added `WzImageCanvasPayloadDecoder` for direct zlib Canvas payloads with
  format `2` / `2562`.
- Added deterministic WzLib tests for successful zlib decode, unsupported
  compression, unsupported format, and unexpected decompressed length.
- Kept decoder integration separate from inspect metadata and Core export.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
