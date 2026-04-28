# 2026-04-28 - Milestone 2 Closeout

## Completion Definition

Milestone 2 is complete. WCX now has a real WC-derived parser path for the
smallest useful resource inspection workflow, and every accepted parser behavior
is reachable through `inspect`. Development-only parser metadata is exposed
through `inspect --debug`.

## Migrated Coverage

- PKG1 and PKG2 package header detection.
- PKG1 directory enumeration.
- Recursive PKG1 directory entries.
- PKG1 name decoding, including `0x02` string-reference names.
- WZ version and hash-offset calculation.
- Directory string keys: none, KMS, GMS, and auto selection.
- IMG root object type detection.
- Bounded `Property` traversal with scalar and nested property values.
- Vector, Convex2D, UOL, Canvas, RawData, Video, and Sound metadata.
- Lua IMG inspection.
- WC text-format IMG v1/v2 inspection.

## Out Of Scope

- PNG export.
- Full Canvas pixel decode matrix.
- Audio and video payload decoding.
- Full PKG2 directory parsing.
- UI browsing.

## Real Client Smoke Coverage

The local read-only GMS client data root is documented in
`docs/format-notes.md`. No client files are stored in git.

- `<local GMS Data>/Base/Base.wz`
  - `dotnet run --project src/WzComparerX.Cli --no-build -- header <file>`
  - `dotnet run --project src/WzComparerX.Cli --no-build -- inspect --debug --key auto <file>`
  - Confirmed valid PKG1 header, encrypted version `57`, WZ version `264`,
    hash version `54037`, PKG1 top-level directory enumeration, recursive
    directory offsets, and auto string-key selection.
- `<local GMS Data>/Base/Base_000.wz` with `StandardPDD.img`
  - `dotnet run --project src/WzComparerX.Cli --no-build -- inspect --debug --key auto --depth 2 <file> StandardPDD.img`
  - Confirmed IMG root `Property`, selected entry metadata, nested property
    traversal, scalar `int32` values, and depth-bounded expansion.
- `<local GMS Data>/UI/UI_000.wz` with `Basic.img` and selected UI images
  - `dotnet run --project src/WzComparerX.Cli --no-build -- inspect --debug --key auto --depth 3 <file> Basic.img`
  - Confirmed Canvas metadata, Canvas direct-zlib/chunked payload metadata,
    Vector child metadata, UOL values, and unsupported Canvas pixel diagnostics.
  - Selected UI images also confirmed Vector-heavy property trees. Local sampled
    UI entries did not expose a compact RawData or Video value; those parser
    paths remain covered by deterministic tests.
- `<local GMS Data>/Sound/Sound_000.wz` with `Bgm00.img`
  - `dotnet run --project src/WzComparerX.Cli --no-build -- inspect --debug --key auto --depth 2 <file> Bgm00.img`
  - Confirmed `Sound_DX8` metadata, payload offsets/lengths, duration fields,
    and expected unsupported audio diagnostics.
- `<local GMS Data>/UI/WZ2Lua/WZ2Lua.wz`
  - `dotnet run --project src/WzComparerX.Cli --no-build -- inspect --debug --key auto <file>`
  - Directory-only smoke. This local package did not expose a direct `.lua`
    image through the current supported directory path.
- Lua IMG inspection
  - Covered by committed synthetic fixtures and golden outputs.
  - Still needs direct real-sample verification when a suitable local entry is
    found.
- Text-format IMG inspection
  - Current sampled local GMS files did not expose a direct text-format IMG
    sample. The behavior is covered by committed synthetic fixtures and golden
    outputs.
  - Still needs direct real-sample verification when a suitable local entry is
    found.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- `rg --hidden -n --glob '!.git' "<retired diagnostic term alternatives>" .`
- `find . -iname '<retired diagnostic term alternatives>' -print`

## Notes

M3 continues from this point with export polish, diagnostics hardening, and the
first narrow Canvas decode/export slices. M4 should wait until inspect/export
models are stable enough for UI browsing.
