# 2026-05-03 MS Outlink Canvas Preview

## Summary

- Fixed Canvas preview for `.ms` / `.mn` image entries so the viewer no longer
  tries to open an MS container through the WZ-only image loader.
- Added a Core MS image inspection loader that exposes the decrypted image
  payload stream to Canvas preview while keeping App view models out of parsing.
- Extended logical link resolution to search MS/MN containers:
  - current `.ms` / `.mn` container first;
  - existing WZ logical package candidates;
  - `Data/Packs/<type>*.ms` / `.mn` candidates.
- Added deterministic Core and App tests for direct MS Canvas preview and MS
  `_outlink` preview.

## WC Alignment Notes

WC opens `.ms` / `.mn` through `Wz_Structure.LoadMsFile`, and `Ms_File` /
`Ms_FileV2` project slash-separated entry names into the same node tree used by
WZ packages. Canvas link display uses `GetLinkedSourceNode` plus
`PluginManager.FindWz(path)` for `source` / `_outlink`, so the target is a
global logical WZ-style path rather than "always the current package".

WCX now follows that shape for preview resolution: a link such as
`Mob/_Canvas/1150000.img/move/0` can resolve inside the current MS container
when present, or through the matching WZ package group under the current
workspace. This is why the local GMS `Mob_00000.ms` smoke can land on
`Data/Mob/_Canvas/_Canvas_000.wz` while still being considered WC-aligned.

Directory parsing remains aligned with WC's current model:

- opening a direct `.ms` / `.mn` file reads entries into slash-separated tree
  nodes, matching `Ms_File.GetDirTree`;
- opening a WZ package group still uses `.ini` `LastWzIndex` and numbered shard
  merge for WZ packages, preserving each image node's true source package;
- WC only auto-loads `Packs/*.{ms,mn}` from the KMST1125/Base special path, so
  WCX keeps Packs lookup explicit through folder/package open and through the
  logical link resolver instead of globally grafting Packs into every WZ tree.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- Targeted Core tests for direct MS Canvas preview and MS `_outlink` preview.
- Targeted App ViewModel test for selecting an MS `_outlink` node and loading
  linked Canvas preview.
- Optional local GMS smoke:
  `WCX_GMS_DATA_DIR=<local-gms-data> dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~CanvasOptionalGmsMsPackImage_ResolvesOutlinkPreview`
