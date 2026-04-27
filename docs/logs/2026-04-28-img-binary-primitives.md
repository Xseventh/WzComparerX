# 2026-04-28 - IMG Binary Reader Primitives

## Summary

- Extracted shared IMG binary read helpers into
  `WzImageBinaryReaderPrimitives`.
- Reused the shared helpers from the binary IMG reader and payload metadata
  reader.
- Removed duplicated compressed integer, little-endian, byte-array, and skip
  helpers before the next object-reader split.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
