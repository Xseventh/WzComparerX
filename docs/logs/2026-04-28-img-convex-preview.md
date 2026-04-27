# 2026-04-28 IMG Convex Preview

## Summary

- Added IMG object-value preview for `Shape2D#Convex2D`.
- Reused the existing Vector preview model for Convex points.
- Added deterministic coverage for valid Convex2D values and non-vector point
  rejection.

## Validation

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-img --depth 4 "/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/UI/UI_000.wz" Basic.img
```

## Notes

- WC requires Convex2D entries to contain only Vector2D objects; WCX now keeps
  that validation in the preview reader.
- The local `Basic.img` smoke primarily exercises Canvas and Vector values; the
  Convex2D path is covered by synthetic fixture bytes.
