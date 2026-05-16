# 2026-05-16 Canvas DXT5 Preview

- Added direct-zlib DXT5 (`format=2050`) Canvas preview support.
- Kept the implementation in Core viewer conversion: WzLib still owns zlib
  payload extraction, while Core converts supported Canvas texture payloads into
  BGRA8888 for the Avalonia viewer.
- Ported the DXT5 block expansion behavior to match WC's `Wz_Png.ExtractPng`
  path: alpha table, alpha indices, RGB565 color table, and color indices are
  decoded into BGRA pixels.
- Kept raw Canvas export unchanged; DXT5 preview support does not imply PNG or
  general media export support.
- Updated the viewer unsupported-format diagnostic to mention the current
  supported preview formats.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
  - App.Tests: 58 passed.
  - Core.Tests: 111 passed.
  - WzLib.Tests: 66 passed.
