# 2026-05-17 Image Entry Not Found Diagnostic

## Context

M5 is tightening the stable diagnostics model before Compare/Search/export
build more behavior on top of Core inspection. Missing IMG selectors were still
surfacing as raw `InvalidDataException` text from Core loaders.

## Changes

- Added stable diagnostic code `wcx.inspection.image.notFound`.
- Routed WZ and MS/MN image selector misses through `ResourceInspectionException`
  so CLI output uses the shared diagnostic formatter.
- Updated App image-content and Canvas-preview workflows to surface
  `ResourceInspectionException` diagnostics instead of dropping them as raw
  activity text.
- Added deterministic CLI and diagnostics factory tests.
- Updated diagnostics docs and the M5 parser coverage matrix.

## Notes

- This does not change binary parser behavior; it only stabilizes the
  resource-selection failure surface.
