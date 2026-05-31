# Canvas Decode And Export Plan

This plan defines the smallest useful Canvas pixel decode/export loop. It is
intentionally narrow so parser metadata, payload decoding, and export stay
separate.

## Boundaries

Keep three layers distinct:

- Parser metadata: existing IMG inspection reads Canvas dimensions, format,
  scale, pages, payload offset/length, compression kind, and expected
  uncompressed length.
- Payload decoder: a future WzLib component turns Canvas payload bytes into a
  decoded pixel/bitmap buffer for supported formats.
- Export: Core selects a decoded payload, chooses content type/file output, and
  reports diagnostics without owning binary parsing details.

Do not make `inspect --debug` depend on successful pixel decoding. Inspection
metadata should remain available even when payload decoding is unsupported.

## Minimum Supported Scope

Start with the smallest deterministic set:

- Direct zlib Canvas payloads detected as `WzImageCanvasCompressionKind.Zlib`.
- One uncompressed BGRA/RGBA-like format only after it is verified against WC
  behavior and a synthetic fixture.
- Single-page images first.
- `export --type canvas --out <path>` or a similarly explicit binary export
  shape. Binary image data must not be written to text stdout by default.

Non-goals for the first loop:

- Chunked encrypted zlib payloads.
- Multi-page Canvas payloads.
- RawData/Video/Sound payload decoding.
- Full real-client format coverage.
- Full UI rendering. A later M4 slice may reuse the same decoder for a narrow
  preview path.

## Fixture Strategy

Before expanding real-client Canvas export, keep committed synthetic fixture
coverage:

- A tiny generated PKG1 IMG containing one Canvas object. The first direct-zlib
  sample is stored as reviewable hex in
  `fixtures/synthetic/canvas-zlib.pkg1.hex`.
- Payload bytes small enough to review in tests.
- Known decoded dimensions and pixel values.
- CLI golden tests for:
  - `inspect --debug` metadata,
  - successful Canvas export to `--out`,
  - unsupported format/compression diagnostics.

Real MapleStory client files may be used for local smoke testing, but must stay
out of git. Record only local paths or observations in local-only notes.

## Diagnostics

Use the diagnostics model in `docs/diagnostics.md`.

Expected new diagnostic families:

- Unsupported Canvas compression:
  `wcx.export.canvas.compressionUnsupported`.
- Unsupported Canvas format:
  `wcx.export.canvas.formatUnsupported`.
- Canvas payload decode failure:
  `wcx.export.canvas.decodeFailed`.
- Canvas export requiring `--out` if binary stdout is not supported:
  `wcx.export.binary.outRequired`.

Add stable `wcx.canvas.*` or `wcx.export.canvas.*` codes before exposing the
conditions through CLI output.

## Suggested First Implementation Slice

1. Add a WzLib Canvas payload decoder interface/model for decoded pixels.
2. Add a synthetic fixture builder/test helper for one tiny zlib Canvas.
3. Add WzLib tests for successful decode and unsupported compression/format.
4. Add Core export type for Canvas with byte-oriented output and diagnostics.
5. Add CLI golden tests for `export --type canvas --out`.
6. Run real-client smoke tests only after synthetic coverage is deterministic.

Status: steps 1 through 5 started with direct-zlib format `2` / `2562` and now
share a WzLib Canvas bitmap decoder for both Preview and CLI export. The shared
decoder converts direct-zlib `ARGB4444` (`1`), `ARGB1555` (`257`), `RGB565`
(`513`), `R16` (`769`), `ARGB8888` (`2`), `A8` (`2304`), `RGBA1010102`
(`2562`), `DXT3` (`1026`), `DXT5` (`2050`), `DXT1` (`4097`), `BC7` (`4098`),
and `RGBA32Float` (`4100`) into BGRA8888 bytes. PNG export and non-direct-zlib
Canvas payloads remain later user-facing image export work. `RGB565` also
supports WC's `scale=4` / `ActualScale=16` case by expanding each source pixel
into a 16x16 block, matching WC's `ImageCodec.ScalePixels` path.
Canvas export now supports the explicit value selector described in
`docs/canvas-export-selector-plan.md` and routes through the same Core Canvas
image service as Avalonia Preview, with export-specific diagnostics layered on
top. That keeps `.ms` / `.mn` payload extraction, package-group image selector
fallback, and Canvas link resolution aligned across CLI and UI.

M5 note: the format conversion logic is no longer a Preview-only path. WzLib
owns Canvas payload decompression and BGRA8888 bitmap conversion; Core owns the
shared Canvas selection/link-resolution workflow and maps the same failures into
viewer or export diagnostics.
