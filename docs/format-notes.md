# Format Notes

This file is a working notebook for MapleStory resource format observations.
It should grow alongside fixtures and tests.

## Known Resource Families

- Classic WZ files.
- Extended WZ layouts.
- MS/MN-style files.
- PKG2 variants.
- Text-format IMG files.
- Canvas image payloads.
- Sound payloads.
- Video payloads.
- Spine animation resources.

## Open Questions

- Which minimum fixture set can cover the first useful WZ browser milestone?
- Which WZ versions can be represented with small synthetic files?
- Which cases require real client-derived samples?
- How should sensitive or copyrighted resource fixtures be handled?
- Can fixtures be minimized to metadata-only or generated payloads?

## Fixture Policy

Fixtures should be:

- Small.
- Legal to store in the repository.
- Named by the behavior they cover, not by client version alone.
- Paired with expected output where possible.

If a real client sample is required, keep it outside git and document how to
place it locally.

## Behaviors To Cover First

- File header/version detection.
- Directory enumeration.
- Image node lazy extraction.
- Scalar property parsing.
- Vector parsing.
- UOL/link resolution.
- Canvas metadata.
- PNG decode for one simple format.
- String search.
- XML/JSON dump stability.

## Notes From WC

WC currently supports multiple compatibility paths in `WzComparerR2.WzLib`,
including newer PKG2-related changes and KMST/KMS-specific format changes. WCX
should treat those paths as reference behavior and add tests before reshaping
the code.

## WZ Header Detection

Initial WCX support covers only package header detection:

- `PKG1` and `PKG2` signatures are recognized.
- Header fields are little-endian, matching WC's `Wz_File.GetHeader` path:
  signature, `Int64` data size, `Int32` header size, and copyright bytes.
- PKG1 directory data starts after the two-byte encrypted version unless WC's
  missing-encver heuristic detects a removed encrypted-version field.
- PKG2 stores two `UInt32` hash fields immediately after the copyright area.

This does not yet validate WZ version profiles, decrypt directory strings, or
read the directory tree.

## Local MapleStory Client Smoke Path

A local MapleStoryNA client was found at:

```text
~/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data
```

This path is useful for manual smoke tests only. Do not commit files from it.
Initial header scans show current local files use `PKG1` headers with 60-byte
package headers, including `Data/Base/Base.wz` and `Data/Base/Base_000.wz`.

`Data/Base/Base.wz` can be raw-previewed without name decryption. It currently
contains 16 top-level directory entries (`nodeType` `0x03`) with zero data size
and checksum fields. This confirms the PKG1 directory entry shape before WZ
version/hash and string decryption are migrated.
