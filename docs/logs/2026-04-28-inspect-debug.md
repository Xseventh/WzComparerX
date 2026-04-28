# 2026-04-28 - Inspect Debug

## Summary

Consolidated migration diagnostics under `inspect --debug` so `inspect` is the
single CLI path into the Core resource inspection model.

## Changes

- Added structured inspection metadata and diagnostics records.
- Added `ResourceInspectionOptions` with string-key, depth, and debug settings.
- Added `inspect --debug` CLI support.
- Projected directory diagnostics into debug metadata:
  - node type,
  - data size,
  - checksum,
  - hash offset position,
  - hash offset,
  - calculated offset,
  - selected string key,
  - WZ/hash version.
- Projected IMG diagnostics into debug metadata:
  - selected entry fields,
  - object type and object value metadata,
  - property type/kind,
  - Canvas/RawData/Video/Sound payload offsets and lengths.
- Added diagnostics for known unsupported payload decoding and full Lua export.
- Removed obsolete diagnostic CLI surfaces and Core-only scaffolding while
  preserving WzLib parser behavior.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- `inspect fixtures/synthetic/basic-tree.json`
- `inspect --debug --key auto` on local `Data/Base/Base.wz`
- `inspect --debug --key auto --depth 2` on local `Data/Base/Base_000.wz`
  `StandardPDD.img`
- Obsolete diagnostic command names return CLI usage with exit code `2`.

## Notes

Normal `inspect` output remains compact. Debug-only fields are only emitted when
`--debug` is requested.
