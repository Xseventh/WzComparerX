# 2026-05-31 Canvas Export Core Service Alignment

## Summary

- Routed `export --type canvas` through the same Core Canvas image service used
  by Avalonia Preview instead of keeping a separate WZ-only export path.
- Kept surface-specific diagnostics by letting the shared service produce
  export diagnostics for CLI export and viewer diagnostics for UI Preview.
- Added an export link diagnostic code for unresolved Canvas links:
  `wcx.export.value.linkUnresolved`.

## Validation

- Added deterministic tests for:
  - CLI Canvas export from a synthetic `.ms` container image.
  - Core Canvas export through an `.ms` `_outlink` target.
- Ran:

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1 --filter "ExportCanvasFromMsWithOut_WritesRawPixelsToFile|ExportCanvas_ResolvesMsOutlinkCanvasPixels|ExportCanvasWithOut_WritesRawPixelsToFile|CanvasImageService_ResolvesMsOutlinkCanvasPixels"
dotnet run --project src/WzComparerX.Cli --no-build -- export --type canvas --out /tmp/wcx-ms-outlink-canvas.bin --value move/0/_outlink --key none "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Packs/Mob_00000.ms" Mob/1150000.img
wc -c /tmp/wcx-ms-outlink-canvas.bin
```

The local GMS `.ms` smoke exported 24,960 BGRA8888 bytes through the CLI path,
matching the UI-preview-capable resolver path for that linked Canvas.

## Notes

- CLI and UI Canvas pixel production now differ only in presentation and
  diagnostic source/code, not in parser, selector, link-resolution, or payload
  decode capability.
- Text and Lua export still use their existing image inspection path. Align
  those with `.ms` / `.mn` only when a real downstream need or sample appears.
