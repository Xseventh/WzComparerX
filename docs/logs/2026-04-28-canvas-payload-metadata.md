# 2026-04-28 - Canvas Payload Metadata

## Summary

Expanded Canvas inspection metadata with the payload compression kind and expected
uncompressed data length for known WC texture formats.

## Changes

- Added Canvas compression kind metadata:
  - direct zlib payloads,
  - chunked encrypted zlib payloads,
  - unknown short payloads.
- Added WC-aligned uncompressed data length estimates for common texture
  formats and scale/page values.
- Updated Canvas text output to include compression and uncompressed byte
  length.
- Added deterministic WzLib tests for zlib and chunked encrypted zlib metadata.
- Documented the behavior as a precursor to future Canvas pixel decoding.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local smoke:
  - `inspect --key auto --depth 2` on local `Data/UI/UI_000.wz` `Basic.img`

The local smoke shows Canvas entries with `compression=Zlib` and expected
uncompressed sizes such as `2` bytes for `1x1` `ARGB4444` frames.
