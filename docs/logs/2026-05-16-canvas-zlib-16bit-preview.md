# 2026-05-16 Canvas Zlib 16-bit Preview

- Checked WC reference behavior in `Wz_Png.ExtractPng`: direct zlib is only the
  payload compression step, and Canvas preview still depends on the texture
  format conversion.
- Expanded WCX Canvas preview conversion for direct-zlib 16-bit formats:
  `ARGB1555` (`257`) and `RGB565` (`513`) now convert to BGRA8888 alongside the
  existing `ARGB4444` (`1`) and `ARGB8888` (`2`) viewer path.
- Kept raw Canvas export scoped to its existing byte-oriented contract; this
  iteration is viewer pixel conversion, not PNG export or full texture matrix
  support.
- Added WzLib decoder tests for 16-bit direct-zlib raw payload acceptance and
  Core tests for BGRA8888 conversion.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
