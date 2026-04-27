# 2026-04-28 Binary IMG Reader Split

## Summary

- Split binary IMG property/object parsing out of `WzImageInspectionReader` into
  `WzImageBinaryInspectionReader`.
- Kept `WzImageInspectionReader` as the image-entry dispatcher for text IMG,
  Lua IMG, and binary IMG stream shapes.
- Preserved existing binary object metadata behavior for Property, Vector,
  Convex2D, UOL, Canvas, RawData, Canvas#Video, and Sound_DX8.

## Notes

`WzImageBinaryInspectionReader` is still the largest parsing hotspot. The next
safe split is to extract Canvas/RawData/Video/Sound payload metadata readers
from the binary reader.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed.
