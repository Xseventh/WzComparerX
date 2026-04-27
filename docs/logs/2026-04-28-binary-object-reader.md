# 2026-04-28 - Binary Object Reader

## Summary

- Split binary IMG entry coordination from binary object/property parsing.
- Kept `WzImageBinaryInspectionReader` as a small IMG-level coordinator.
- Moved binary object type dispatch, scalar property reading, nested object
  parsing, vectors, convex values, UOL, and mini-property handling into
  `WzImageBinaryObjectInspectionReader`.
- Preserved existing payload metadata delegation to
  `WzImagePayloadInspectionReader`.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`

## Next

- Split nested property recursion out of `WzImageBinaryObjectInspectionReader`
  before adding more binary IMG object types.
