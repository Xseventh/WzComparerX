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
and checksum fields. This confirms the PKG1 directory entry shape.

The same file's top-level names decode with the no-op PKG1 string key
(`--key none`; WC historically names this `BMS`).
The default `preview-dir` path now uses that key and can list names such as
`Character`, `Effect`, `Etc`, `Item`, `Map`, `Mob`, `Npc`, `String`, and `UI`.
KMS/GMS key modes are still exposed for older or region-specific files.
`preview-dir --key auto` also selects the no-op key for this local GMS
`Data/Base/Base.wz` smoke file.

PKG1 version detection resolves the local `Data/Base/Base.wz` header as WZ
version `264` with hash version `54037`. Its top-level directory offsets resolve
to byte positions `360` through `375`, a compact sequence of empty child
directory tables immediately after the top-level directory table.

`Data/Base/Base_000.wz` contains image entries such as `smap.img`,
`StandardPDD.img`, and `zmap.img`. Their calculated offsets point to IMG
payloads whose top-level object type currently reads as `Property`.
`smap.img` previews as 151 first-layer properties, mostly string mappings and
null placeholders. `StandardPDD.img` previews as six nested `Property` objects.
With `preview-img --depth 2`, those nested `Property` objects expand into
scalar child entries such as integer threshold values.

`Data/UI/UI_000.wz` contains UI image entries such as `Basic.img`. With
`preview-img --depth 2`, many nested Canvas values now preview as metadata:
width, height, texture format, scale, page count, payload offset, and payload
length. With `--depth 3`, Canvas mini-properties expose child values such as
`origin` vectors.

The same object readers are also used when an IMG root object is a supported
non-`Property` type. For example, top-level Canvas and `Shape2D#Vector2D`
objects produce direct `objectValue` metadata instead of only printing their
object type.

`Data/Sound/Sound_000.wz` contains sound image entries such as
`AchievementEff.img` and `Bgm00.img`. With `preview-img --depth 2`, their
`Sound_DX8` values preview as metadata including duration, sound declaration,
payload length, and payload offset. Audio payload decoding is not implemented.
