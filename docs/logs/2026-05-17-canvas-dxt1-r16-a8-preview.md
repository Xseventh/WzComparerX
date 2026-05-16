# 2026-05-17 Canvas DXT1 R16 A8 RGBA32Float Preview

- Added direct-zlib Canvas preview support for WC texture formats:
  - `R16` (`format=769`), converted to BGRA8888 as a red-channel preview.
  - `A8` (`format=2304`), converted to BGRA8888 as a white alpha-mask preview.
  - `DXT1` (`format=4097`), decoded from 8-byte BC1 blocks into BGRA8888.
  - `RGBA32Float` (`format=4100`), converted from RGBA float channels into
    clamped BGRA8888 preview pixels.
- Kept the parser/export boundary unchanged: raw Canvas export still only
  supports the existing `format=2` / `format=2562` byte-export slice, while the
  new formats are viewer conversion slices.
- Left `BC7` (`format=4098`) as the remaining heavier media/rendering slice.
  `4100` length metadata remains covered by the previous fixture.

Verification:

- Targeted Core Canvas preview tests.
- Targeted WzLib Canvas payload decoder tests.
