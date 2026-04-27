# 2026-04-28 - Payload Inspection Reader

## Summary

- Extracted Canvas/RawData/Video/Sound payload metadata parsing from
  `WzImageBinaryInspectionReader` into `WzImagePayloadInspectionReader`.
- Kept object dispatch, mini-property parsing, and nested property recursion in
  the binary IMG reader so this split does not change parser behavior.
- Left payload decoding unsupported; this only preserves existing metadata
  inspection for offsets, lengths, Canvas compression kind, and sound headers.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`

## Next

- Split binary object value dispatch and nested property recursion before adding
  more IMG object types.
- Keep CLI-visible inspect/export output covered by golden tests as parser
  coverage grows.
