# Migration From WzComparerR2

This document tracks how WzComparerX should reuse WzComparerR2 without becoming
a line-by-line rewrite of it.

## Source Project Names

- WC: WzComparerR2, the maintained existing implementation.
- WCX: WzComparerX, the new architecture described in this repository.

## Migration Principles

- Preserve format knowledge before preserving UI behavior.
- Prefer small, testable ports over large copied modules.
- Keep Windows-only dependencies out of the core.
- Add tests around migrated behavior as soon as a fixture exists.
- Treat old public surface area as reference material, not as a compatibility
  contract.

## High-Value WC Assets

### WzComparerR2.WzLib

Highest priority. It contains WZ/MS parsing, version detection, encryption,
image extraction, checksums, and compatibility logic.

Porting approach:

1. Copy a minimal subset into `WzComparerX.WzLib`.
2. Remove UI and framework-specific assumptions.
3. Keep behavior close to WC until tests prove safe refactors.
4. Add fixture-backed tests for each supported format case.

### Comparer

Worth migrating after basic browsing works. WC already contains compare logic
for virtual WZ nodes and report generation.

Porting approach:

1. Extract comparison model first.
2. Separate comparison computation from HTML/report rendering.
3. Add JSON output before adding UI diff views.

### CharaSim Domain Models

Valuable but broad. Item, gear, skill, familiar, damage skin, and set item logic
should move into explicit domain models.

Porting approach:

1. Start with read-only models.
2. Avoid moving custom WinForms tooltip renderers early.
3. Add tooltip data projection before visual rendering.

### Avatar

Important user-facing feature, but not part of the first milestone.

Porting approach:

1. Separate data assembly from drawing.
2. Define a frame/layer output model.
3. Add rendering after the data model stabilizes.

### MapRender

Large and high-risk. It should be treated as a separate module.

Porting approach:

1. Port map data loading separately from rendering.
2. Define a scene graph model independent of MonoGame.
3. Choose rendering backend later.

## Areas To Avoid Porting Directly

- `MainForm` orchestration and event wiring.
- Designer-generated WinForms UI.
- Global `PluginManager.FindWz` as the primary resource lookup mechanism.
- Direct DotNetBar dependencies.
- Direct SharpDX dependencies in shared logic.
- WindowsDX-specific assumptions in core or domain projects.

## Compatibility Checklist

For each migrated feature, record:

- WC source files referenced.
- WC behavior being preserved.
- Fixture or sample used.
- Test coverage added.
- Known unsupported cases.
- UI dependency removed or isolated.

## Migrated Behaviors

### WZ Package Header Detection

- WC source files referenced:
  - `WzComparerR2.WzLib/Wz_Header.cs`
  - `WzComparerR2.WzLib/Wz_File.cs`
- WC behavior preserved:
  - Recognize `PKG1` and `PKG2` signatures.
  - Read data size, header size, copyright, and directory start position.
  - Preserve WC's PKG1 encrypted-version-missing heuristic for values above
    `0xff` and the compressed-int `0x80` edge case.
  - Read PKG2 header hash fields.
- Fixture or sample used:
  - Minimal in-memory synthetic WZ header byte streams in
    `WzPackageHeaderReaderTests`.
- Test coverage added:
  - Valid PKG1 with encrypted version.
  - PKG1 with missing encrypted version.
  - Valid PKG2 hash fields.
  - Invalid signature handling.
- Known unsupported cases:
  - Header detection itself does not validate version profiles; PKG1 and the
    first PKG2 KMST1199/1200 directory/profile slices are tracked in later
    migrated-behavior sections.
  - MS/MN containers and `List.wz` are not WZ header formats and are tracked
    separately.
- UI dependency removed or isolated:
  - Implemented in `WzComparerX.WzLib` only; no UI dependency.

### PKG1 Directory Inspection And Names

- WC source files referenced:
  - `WzComparerR2.WzLib/Wz_File.cs`
  - `WzComparerR2.WzLib/Utilities/WzBinaryReader.cs`
  - `WzComparerR2.WzLib/Wz_Crypto.cs`
  - `WzComparerR2.WzLib/Compatibility/WzOffsetCalc.cs`
  - `WzComparerR2.WzLib/Compatibility/WzVersionVerifier.cs`
  - `WzComparerR2.WzLib/Utilities/MathHelper.cs`
- WC behavior preserved:
  - Read PKG1 compressed entry count.
  - Read top-level directory/image entry type, size, checksum, and raw hash
    offset fields.
  - Decode inline PKG1 ASCII/UTF-16 directory strings with no-op, KMS, and
    GMS key modes.
  - Decode PKG1 `0x02` string-reference entry names.
  - Calculate PKG1 hash versions and entry offsets.
  - Detect missing-encrypted-version PKG1 files as WZ version `777`.
  - Detect encrypted-version PKG1 files by validating calculated offsets.
  - Enumerate recursively nested PKG1 directory tables in WC's linear layout.
  - Read the top-level IMG object type name from calculated image offsets.
  - Inspect supported top-level non-`Property` IMG object values.
  - Read first-layer `Property` entries, including simple scalar values and
    nested object summaries.
  - Expand nested IMG `Property` objects to a caller-provided bounded depth.
  - Decode the supported scalar property tags currently covered by fixtures:
    null, int16 aliases, compressed/expanded int32, alternate int32 tag,
    compressed/expanded int64, compressed/expanded single, double, string, and
    empty nested `Property` nodes.
  - Inspect nested `Shape2D#Vector2D`, `Shape2D#Convex2D`, `UOL`, Canvas
    metadata, RawData metadata, Canvas#Video metadata, and Sound_DX8 metadata

### PKG2 KMST1199/1200 And Modern KMS Directory Inspection

- WC source files referenced:
  - `WzComparerR2.WzLib/Wz_File.cs`
  - `WzComparerR2.WzLib/Wz_Header.cs`
  - `WzComparerR2.WzLib/Utilities/WzBinaryReader.cs`
  - `WzComparerR2.WzLib/Wz_Crypto.cs`
  - `WzComparerR2.WzLib/Compatibility/WzDirStringReader.cs`
  - `WzComparerR2.WzLib/Compatibility/WzOffsetCalc.cs`
  - `WzComparerR2.WzLib/Compatibility/WzVersionProfile.cs`
  - `WzComparerR2.WzLib/Compatibility/WzVersionVerifier.cs`
- WC behavior preserved:
  - Read PKG2 encrypted entry counts and offset counts.
  - Recognize modern KMS 0x44-byte PKG2 header envelopes that scatter `hash1`,
    check value, and data size instead of using the older literal `PKG2`
    header layout.
  - Decode KMST1199/1200 first-entry directory names with the PKG2 UTF-16
    directory string key derived from `hash1` and `hashVersion`.
  - Decode modern KMS first-entry directory names with the same PKG2 UTF-16
    string mechanism and the fixed observed hash version; WCX leaves
    `wzVersion` unset for this profile because the envelope does not expose a
    traditional WZ version.
  - Decode later names in the same directory level through the normal
    PKG1-style string reader.
  - Decrypt KMST1199/1200 and modern KMS entry counts and calculate image
    offsets with WC's `Pkg2OffsetCalcV3` formula.
  - Project PKG2 image entries through the same Core inspection identity and
    IMG extraction path as PKG1 images.
- Fixture or sample used:
  - Synthetic `pkg2_kmst1200` and modern KMS package bytes generated in
    `tests/TestSupport/Pkg2PackageFixture.cs`.
  - User-supplied local KMS/KMST-style `Item_000.wz` sample in `~/Downloads`
    for manual smoke only; the file is not committed.
  - User-supplied local modern KMS samples under `~/Downloads/new_kms/` for
    manual smoke only; the files are not committed.
- Test coverage added:
  - WzLib synthetic directory inspection test for KMST1200 names, profile, hash
    version, and image offsets.
  - WzLib/Core synthetic coverage for modern KMS header reading, directory
    profile selection, nullable WZ version, and image offsets.
  - Core inspection tests for synthetic PKG2 directory projection and image
    payload inspection.
  - CLI debug smoke tests for synthetic KMST1200 and modern KMS directory
    output, including `pkg2HeaderVariant: modern`.
- Known unsupported cases:
  - KMST1196-1198 legacy PKG2 profiles are still unsupported.
  - Unsupported PKG2 profile/container shapes return
    `wcx.package.pkg2.directoryUnsupported`.
  - This slice does not add new IMG value decoding beyond the existing shared
    IMG readers.
- UI dependency removed or isolated:
  - Implemented in `WzComparerX.WzLib` and Core inspection services only; no UI
    dependency.

### List.wz String-List Inspection

- WC source files referenced:
  - `WzComparerR2.WzLib/Wz_Crypto.cs`
- WC behavior preserved:
  - Treat `List.wz` as a helper string list, not a resource package tree.
  - Read records as `int32` character count plus UTF-16LE-shaped bytes and a
    2-byte terminator.
  - Decode the low byte of each UTF-16 code unit with the selected PKG1 key
    stream, matching WC's `LoadListWz` loop.
  - Auto-detect GMS/KMS/no-op keys for synthetic coverage; WC detects GMS/KMS
    by checking whether the first decrypted character is `d`.
  - Exclude the `dummy` sentinel from the effective entries.
- Known differences / deferred behavior:
  - WCX exposes `List.wz` through `inspect` as `format: listwz` but does not yet
    feed it into PKG1 string key/profile selection.
  - The current local GMS sample has no `List.wz`; real smoke is waiting for an
    older-client sample.
    object values.
  - Detect Canvas payload compression kind and expected uncompressed data
    length for WC texture formats.
  - Auto-detect inspection string key selection across no-op, KMS, and GMS modes
    using conservative directory-name scoring.
  - Inspect `.lua` IMG entries through WC's Lua-specific block stream path.
  - Inspect WC text-format IMG v1 (`#Property`) and v2 (`Root <Property>`)
    streams.
- Fixture or sample used:
  - Minimal in-memory synthetic WZ byte streams in
    `WzDirectoryInspectionReaderTests`.
  - Local-only MapleStoryNA smoke file `Data/Base/Base.wz`.
- Test coverage added:
  - PKG1 directory entry pre-read with encoded no-op strings.
  - PKG1 `0x02` string-reference names.
  - PKG1 recursive child directory table enumeration.
  - PKG1 hash-version and offset calculation.
  - IMG object type inline and referenced strings.
  - First-layer IMG `Property` scalar entries.
  - PKG1 encrypted-version detection against deterministic synthetic offsets.
  - Core inspection string key auto-detection against deterministic synthetic
    directory data.
  - Core text formatter output for decoded inspection entries.
  - Bounded nested IMG `Property` inspection.
  - IMG Vector, Convex2D, UOL, Canvas metadata, RawData metadata, and
    Canvas#Video metadata value inspection.
  - Canvas zlib/chunked payload metadata and uncompressed data size estimates.
  - IMG Sound_DX8 metadata value inspection.
  - Top-level IMG Canvas metadata and Vector value inspection.
  - Lua image block inspection and `--depth 0` behavior.
  - Text IMG v1/v2 scalar and nested property inspection.
- Known unsupported cases:
  - Full unbounded IMG property/value parsing is not implemented yet.
  - Text IMG v2 multiline string parsing is not implemented yet.
  - Full Canvas pixel decode coverage is not implemented yet; WCX currently has
    a narrow direct-zlib slice used by export and the basic Avalonia preview.
  - RawData payload decoding is not implemented yet.
  - Canvas#Video payload decoding is not implemented yet.
  - Sound_DX8 audio payload decoding is not implemented yet.
  - Other nested IMG object payloads are still summarized by object type.
- UI dependency removed or isolated:
  - Implemented in `WzComparerX.WzLib` and Core CLI services only; no UI
    dependency.

### MS Container Directory Inspection

- WC source files referenced:
  - `WzComparerR2.WzLib/Wz_Structure.cs`
  - `WzComparerR2.WzLib/Ms_File.cs`
  - `WzComparerR2.WzLib/Ms_FileV2.cs`
  - `WzComparerR2.WzLib/Ms_Header.cs`
  - `WzComparerR2.WzLib/Ms_Entry.cs`
  - `WzComparerR2.WzLib/Ms_Image.cs`
  - `WzComparerR2.WzLib/Ms_ImageV2.cs`
  - `WzComparerR2.WzLib/Cryptography/Snow2CryptoTransform.cs`
  - `WzComparerR2.WzLib/Cryptography/ChaCha20CryptoTransform.cs`
  - `WzComparerR2/MainForm.cs`
- WC behavior preserved:
  - Open `.ms` and `.mn` paths through the MS loader path.
  - Try the Snow-based `Ms_File` reader first, then the ChaCha20-based
    `Ms_FileV2` reader when the first shape does not match.
  - Derive the random-byte prefix length from the lowercase file name.
  - Decode the v2/Snow salt, header hash, version, entry count, entry table
    start position, and aligned data start position.
  - Decode the v4/ChaCha20 salt, header hash, entry count, entry table start
    position, and aligned data start position.
  - Read entry table fields: name, checksum, flags, relative block, size,
    aligned size, unknown fields, and per-entry key bytes.
  - Convert entry relative blocks into absolute offsets from the aligned data
    start position.
  - Project slash-separated entry names into a directory tree.
- Fixture or sample used:
  - Synthetic v2/Snow and v4/ChaCha20 `.ms` streams generated in
    `tests/TestSupport/MsContainerFixture.cs`.
  - Synthetic `.mn` stream generated with the same fixture to lock WC's shared
    MS loader path for both extensions.
  - Optional local MapleStoryNA `Data/Packs/*.ms` smoke files discovered through
    the external-client smoke harness. Use `WCX_CLIENT_DATA_DIRS` for one or
    more client `Data` directories.
- Test coverage added:
  - WzLib tests for v2/Snow header and entry table inspection.
  - WzLib tests for v4/ChaCha20 header and entry table inspection.
  - Core tests for projecting `.ms` entries into the shared inspection model.
  - Core and CLI tests for projecting synthetic `.mn` entries through the same
    inspection model.
  - Core/App tests for direct MS Canvas preview and MS `_outlink` Canvas preview
    through the shared logical link resolver.
  - CLI test for debug output on a synthetic `.ms` container.
  - Optional GMS smoke that reads all local `Data/Packs/*.ms` directory tables
    and verifies `Packs/Mob_00000.ms` `_outlink` Canvas preview resolution.
- Known unsupported cases:
  - `.ms` and `.mn` entry payload extraction is implemented for the current
    v2/Snow and v4/ChaCha20 container readers, but unsupported or malformed IMG
    payload shapes still return `wcx.package.ms.imageUnsupported`.
  - Unsupported `.ms` or `.mn` container shapes return
    `wcx.package.ms.directoryUnsupported`.
  - The per-entry key bytes are retained for payload extraction but are not
    exposed in inspection metadata.
  - v2 checksum validation is implemented for the header; v4 hash validation
    remains aligned with WC's current TODO.
- UI dependency removed or isolated:
  - Implemented in `WzComparerX.WzLib` and Core inspection services only; no UI
    dependency.

## Suggested Migration Order

1. Repository and solution skeleton.
2. WZ node model and basic file open.
3. Lazy image extraction.
4. Node listing CLI.
5. XML/JSON dump.
6. Image export.
7. Search.
8. Compare model.
9. Compare report output.
10. Basic Avalonia browser.
11. Domain models for item/gear/skill.
12. Tooltip data projection.
13. Avatar data model.
14. Map data model.
15. Rendering modules.
