# 2026-05-08 PKG2 KMST1200 Directory Slice

## Summary

- Added the first PKG2 directory/profile implementation slice:
  - detects WC-style KMST1199/1200 PKG2 directory profiles;
  - decodes first-entry PKG2 UTF-16 directory strings;
  - decodes later directory names through the normal PKG1-style string reader;
  - decrypts entry counts and calculates image offsets with the KMST1199/1200
    offset algorithm;
  - routes supported PKG2 image entries into the existing IMG inspection path.
- Kept unsupported PKG2 profiles behind the stable diagnostic
  `wcx.package.pkg2.directoryUnsupported`.
- Added deterministic synthetic `pkg2_kmst1200` coverage for WzLib, Core, and
  CLI inspection.

## Real Sample

- Used the user-supplied local KMS/KMST-style file
  `~/Downloads/Item_000.wz` for manual smoke only; the file is not committed.
- Header smoke:
  - `format: pkg2`
  - `headerSize: 60`
  - `directoryStartPosition: 68`
  - `hash1: 0xb5b60cfc`
  - `hash2: 0x48f17f97`
- Directory smoke:
  - detected `formatProfile: pkg2_kmst1200`
  - detected `hashVersion: 0xb0da16f2`
  - listed `ItemOption.img`, `ItemSellPriceStandard.img`, `SkillOption.img`,
    and `ThothSearchOption.img`.
- IMG smoke:
  - `inspect --depth 1 ~/Downloads/Item_000.wz SkillOption.img` returns a
    `Property` root with `skill`, `socket`, and `inc` children.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- Targeted PKG2 tests:
  - WzLib `Read_ReturnsPkg2Kmst1200DirectoryEntries`
  - Core `InspectPkg2Kmst1200Directory_ReturnsEntryTree`
  - Core `InspectPkg2Kmst1200Image_ReadsImagePayload`
  - CLI `InspectDebugPkg2Kmst1200Directory_EmitsEntries`
- `dotnet test WzComparerX.slnx --no-build -m:1`
