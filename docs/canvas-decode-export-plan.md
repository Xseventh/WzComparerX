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

Status: steps 1 through 5 are started for direct zlib Canvas payloads with
format `2` / `2562`, including a committed hex fixture, `inspect --debug`
text/JSON golden outputs, and raw-byte CLI export coverage. This raw-byte path
is sufficient for M3 only as a parser/export slice. PNG export and broader
Canvas format coverage are later user-facing image export work. Canvas export
now supports the explicit value selector described in
`docs/canvas-export-selector-plan.md`.

M4 note: the basic Avalonia preview reuses the same payload decoder and adds a
small viewer conversion path for direct-zlib format `1` / `2` Canvas values.
This does not change the current raw Canvas export contract.
