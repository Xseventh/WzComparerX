# 2026-05-17 Canvas RGB565 Scale 16 Preview

- Added direct-zlib Canvas preview support for WC's RGB565 scale expansion case:
  `format=513`, `scale=4`, `ActualScale=16`.
- Kept the behavior in the viewer conversion layer. WzLib now preserves Canvas
  `scale` on decoded raw payloads; Core expands each source RGB565 pixel into a
  16x16 BGRA8888 block, matching WC's `ImageCodec.ScalePixels` path.
- Updated the unsupported scale diagnostic so it no longer says only unscaled
  Canvas values are supported.
- Documented the `RGB565 scale=4` exception in the Canvas decode plan, handoff,
  README, roadmap, and parser coverage matrix.

Validation:

- Added WzLib payload decoder coverage for scaled RGB565 raw bytes.
- Added Core Canvas preview coverage for 16x16 pixel expansion from a two-pixel
  RGB565 source row.
- Re-ran the unified optional external-client Core smoke suite against the local
  GMS `Data` directory; 14 tests passed.
