# 2026-05-16 Canvas RGBA1010102 Preview

- Fixed Canvas uncompressed-length metadata for WC texture format
  `RGBA32Float`: WC uses format code `4100`, not `6656`.
- Added direct-zlib `RGBA1010102` (`format=2562`) Canvas preview support.
- Matched WC's `R10G10B10A2ToBGRA32` conversion: 10-bit RGB channels are
  shifted down to 8-bit values and the 2-bit alpha channel maps to `0/85/170/255`.
- Kept this as a viewer conversion slice. Raw Canvas export already accepts
  `format=2562`; PNG export and broader image export remain later work.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
  - App.Tests: 58 passed.
  - Core.Tests: 113 passed.
  - WzLib.Tests: 68 passed.
