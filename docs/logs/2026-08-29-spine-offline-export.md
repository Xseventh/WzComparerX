# 2026-08-29 Spine Offline Export

## Summary

- Added `export --type spine --out <directory>` for offline Spine resource
  bundles.
- Kept binary payload, atlas, version, and PNG encoding primitives in WzLib;
  Core owns image selection, resource-link resolution, validation, and bundle
  assembly.
- Replaced the Canvas-specific image inspection context with a shared IMG
  context used by Canvas, Video, and Spine workflows.

## Behavior

- Preserves original binary `.skel` or JSON skeleton content and atlas text.
- Reads every texture page declared by the atlas, including multi-page atlases.
- Resolves `source`, `_inlink`, `_outlink`, and `link` Canvas placeholders
  through the same Core path used by Preview and Canvas export.
- Converts decoded BGRA8888 Canvas pixels to standard RGBA PNG files with the
  exact atlas page names.
- Validates safe output paths and atlas-declared dimensions before returning a
  bundle.
- Detects current Spine 4 binary version metadata and the legacy binary version
  placement used by WC's Spine loader family.

## Validation

- Added WzLib tests for exact RawData reads, skeleton version detection,
  multi-page atlas parsing, and PNG channel/dimension output.
- Added a deterministic Core MS fixture that exports a two-page Spine bundle.
- Added CLI output coverage for the complete offline directory contract.
- Added optional external-client smoke for CMS v227.7 Map Object and Back
  samples. It validates Spine 4.1.24, skeleton sizes of 852 and 970 bytes,
  linked full-size textures, and exact multi-page atlas names.
- Client files and generated smoke outputs remain outside the repository.

## Boundary

- This slice exports resources for a matching Spine runtime; it does not parse
  skeleton semantics, render animations, or add an Avalonia Spine player.
- Binary skeletons stored as RawData and JSON skeleton strings are supported.
  Sound-backed legacy skeleton payloads remain sample-driven future work.
- General-purpose Canvas PNG export remains separate; PNG encoding here is part
  of the Spine bundle contract.
