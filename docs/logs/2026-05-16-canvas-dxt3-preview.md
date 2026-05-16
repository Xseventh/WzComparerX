# 2026-05-16 Canvas DXT3 Preview

- Added direct-zlib DXT3 (`format=1026`) Canvas preview support.
- Kept WzLib responsible for zlib payload extraction and Core responsible for
  viewer conversion into BGRA8888 pixels.
- Matched WC's `ImageCodec.DXT3ToBGRA32` behavior: 4-bit alpha nibbles are
  expanded to 8-bit alpha, while the shared DXT RGB565 color table and 2-bit
  color indices are expanded per block.
- Updated the viewer unsupported-format diagnostic and parser coverage docs to
  include DXT3 alongside the existing ARGB4444, ARGB1555, RGB565, ARGB8888, and
  DXT5 viewer slices.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
  - App.Tests: 58 passed.
  - Core.Tests: 112 passed.
  - WzLib.Tests: 67 passed.
