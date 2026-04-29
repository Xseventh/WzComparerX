# 2026-04-29 WC-Style IMG Browser

## Summary

Aligned the M4 resource browser closer to WC's package and IMG browsing model.
Core now composes WC-style package groups above the single-file WzLib parser,
and Avalonia now separates the package Resources tree from the extracted IMG
Content tree.

## Changes

- Added a Core package group loader for `Name.wz` plus numbered shards:
  - reads sibling `Name.ini` and `LastWzIndex` when present;
  - falls back to contiguous `Name_000.wz`, `Name_001.wz`, ... enumeration;
  - merges shard directory entries under the entry package;
  - keeps shard IMG node paths pointing at the original shard package.
- Updated split-folder linking to build linked package groups instead of showing
  each numbered shard as a separate supported surface.
- Added Avalonia IMG Content state and tree:
  - selecting an image node extracts the complete single IMG into IMG Content;
  - the Resources tree remains focused on packages and directories;
  - manual `Inspect Image` refreshes IMG Content rather than replacing
    Resources.
- Changed Canvas preview to follow IMG Content selection:
  - selecting Canvas nodes previews exact values;
  - root Canvas IMG objects still work;
  - selecting `source`, `_inlink`, or `_outlink` string nodes resolves linked
    Canvas values when the current workspace path can be mapped.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local GMS smoke with the built CLI:
  - `Map/Map/Map1/Map1.wz` now returns merged `Map1_000.wz` IMG entries such
    as `100000000.img` under the entry package, with shard targets preserved.

## Notes

The built CLI returns for `Map1.wz`; the earlier long wait came from
`dotnet run` rebuilding while the worktree had transient compile errors. The
new WC-style merge can produce very large directory text output, so future CLI
polish should consider directory output limits or summaries. This is a user
experience issue rather than a parser deadlock.
