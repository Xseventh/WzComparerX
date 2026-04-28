# 2026-04-28 - CLI Depth JSON Regression Test

## Summary

- Added CLI JSON regression coverage for `inspect --debug --json --depth 1`
  on a synthetic PKG1 Canvas IMG with mini-property metadata.
- Locked the structured JSON shape for compact mini-property output:
  `childCount` and Canvas payload metadata stay in `DebugMetadata`, child nodes
  stay empty at the requested depth, and the Canvas unsupported-payload
  diagnostic keeps its stable code.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
