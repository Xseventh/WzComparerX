# 2026-04-28 - Binary Property Reader

## Summary

- Extracted binary IMG property traversal into
  `WzImageBinaryPropertyInspectionReader`.
- Extracted IMG object/property string decoding into
  `WzImageBinaryStringReader`.
- Kept binary object type dispatch in `WzImageBinaryObjectInspectionReader`.
- Preserved the difference between ordinary nested `Property` objects, which
  can be skipped by the object boundary when beyond `--depth`, and
  mini-property blocks, which must still be consumed to locate following
  Canvas/RawData/Video/Sound payload metadata.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`

## Next

- Add more deterministic fixture coverage before expanding binary IMG object
  support further.
- Keep new object-type behavior routed through inspect diagnostics before any
  UI work depends on it.
