# 2026-05-17 Canvas BC7 Preview

- Added direct-zlib Canvas preview support for WC texture format `BC7`
  (`format=4098`) by porting WC's `bc7decomp`-based block decoder into the
  Core viewer conversion layer.
- Kept WzLib responsible for zlib/raw payload extraction only. BGRA8888 pixel
  conversion remains in `ResourceCanvasImageService`, matching the existing
  DXT1/DXT3/DXT5 viewer slices.
- Matched WC's BC7 dimension behavior: decoded preview dimensions use
  `width & ~3` and `height & ~3`, discarding non-4-aligned tail rows/columns.
- Added deterministic tests for:
  - WzLib accepting `format=4098` direct-zlib raw blocks.
  - Core converting a synthetic BC7 mode-6 block to BGRA8888 pixels.
  - WC-style BC7 tail trimming.
  - Optional local GMS smoke for `Skill/_Canvas/_Canvas_097.wz` ->
    `6414.img` -> `skill/64141504/effect/1`.
- Investigated the apparent `_Canvas.wz` hang report. The local GMS
  `Skill/_Canvas/_Canvas.wz` entry package is a 63-byte stub with
  `_Canvas.ini` declaring `LastWzIndex|122`; direct `inspect` against
  `_Canvas.wz` and `6414.img` returns `Image entry not found` quickly, while
  the actual shard `_Canvas_097.wz` inspects `6414.img` quickly. Treat future
  slow paths around entry-package image selectors as selector resolution /
  package-group routing work, not as evidence of an IMG parser infinite loop.

Verification:

- Targeted WzLib BC7 payload decoder test.
- Targeted Core BC7 Canvas preview tests.
- Optional local GMS BC7 smoke through `WCX_CLIENT_DATA_DIR`.
