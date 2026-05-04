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

`Data/Base/Base.wz` can be inspected without name decryption. It currently
contains 16 top-level directory entries (`nodeType` `0x03`) with zero data size
and checksum fields. This confirms the PKG1 directory entry shape.

The same file's top-level names decode with the no-op PKG1 string key
(`--key none`).
The default `inspect` path now uses that key and can list names such as
`Character`, `Effect`, `Etc`, `Item`, `Map`, `Mob`, `Npc`, `String`, and `UI`.
KMS/GMS key modes are still exposed for older or region-specific files.
`inspect --debug --key auto` also selects the no-op key for this local GMS
`Data/Base/Base.wz` smoke file.

PKG1 version detection resolves the local `Data/Base/Base.wz` header as WZ
version `264` with hash version `54037`. Its top-level directory offsets resolve
to byte positions `360` through `375`, a compact sequence of empty child
directory tables immediately after the top-level directory table.

For GMS-style split-package layouts, empty directory entries can be package
index stubs rather than complete nested directories. WCX now resolves matching
package directories in Core inspection when a stub has no parsed children. For
example, `Data/Base/Base.wz` entry `Effect` can expand to linked package nodes
from `Data/Effect/Effect.wz` and `Data/Effect/Effect_*.wz`, while
`Data/UI/UI.wz` entry `_Canvas` can expand to `Data/UI/_Canvas/_Canvas.wz` and
its numbered shards. The broader data workspace lookup is limited to the
`Base/Base.wz` package index shape; linked packages resolve nested stubs
relative to their own package directory, matching WC's folder loading behavior.
Split-package directory trees are loaded eagerly in the WC style, while IMG
payloads remain lazy and selector-driven. Once a single IMG is selected, the UI
requests a full IMG inspection rather than a depth-limited partial tree. The
App consumes the resulting inspection tree directly; it does not implement
separate path guessing.

`Data/Base/Base_000.wz` contains image entries such as `smap.img`,
`StandardPDD.img`, and `zmap.img`. Their calculated offsets point to IMG
payloads whose top-level object type currently reads as `Property`.
`smap.img` inspects as 151 first-layer properties, mostly string mappings and
null placeholders. `StandardPDD.img` inspects as six nested `Property` objects.
With `inspect --debug --depth 2`, those nested `Property` objects expand into
scalar child entries such as integer threshold values.

`Data/UI/UI_000.wz` contains UI image entries such as `Basic.img`. With
`inspect --debug --depth 2`, many nested Canvas values now expose metadata:
width, height, texture format, scale, page count, payload offset, and payload
length. Canvas inspection also reports whether the payload looks like direct zlib
or WC's chunked encrypted zlib stream, plus the expected uncompressed byte
length for known texture formats. With `--depth 3`, Canvas mini-properties
expose child values such as `origin` vectors.

The same object readers are also used when an IMG root object is a supported
non-`Property` type. For example, top-level Canvas and `Shape2D#Vector2D`
objects produce direct `objectValue` metadata instead of only printing their
object type.

WC treats entries whose image names end in `.lua` as a separate stream shape
instead of the normal IMG object-tag stream. Lua blocks use flag `0x01`,
compressed payload length, key-stream-only decryption, and UTF-8 text. WCX now
inspects Lua block count, payload length, and a short text snippet.
`export --type lua` writes the full decoded script for supported Lua IMG blocks.
Multiple Lua blocks are exported by concatenating decoded blocks in stream order
without adding separators.
The local client contains WZ2Lua and `_Canvas` packages with Lua-related
strings, but the currently supported directory inspection paths do not yet
expose a direct local `.lua` smoke entry.

WC also supports text-format IMG streams. V1 starts with `#Property` and uses
`key = value` lines with `{ ... }` property blocks. V2 starts with
`Root <Property>` and uses tab indentation plus explicit node types such as
`<I4>`, `<I8>`, `<R8>`, `<String>`, `<Vector>`, and `<Property>`. WCX inspects
both variants into the same bounded property inspection model. Current local GMS
data did not expose a direct text IMG smoke sample in the scanned WZ files.

`Data/Sound/Sound_000.wz` contains sound image entries such as
`AchievementEff.img` and `Bgm00.img`. With `inspect --debug --depth 2`, their
`Sound_DX8` values inspect as metadata including duration, sound declaration,
payload length, and payload offset. Audio payload decoding is not implemented.

M5 added optional Core smoke tests gated by `WCX_GMS_DATA_DIR`. On the local GMS
client these tests inspect representative package roots for `Character`,
`Effect`, `Item`, `Mob`, `Npc`, `Skill`, `Sound`, `String`, and `UI` without
parser errors. They also validate the `Data/Map/Map/Map1/Map1.wz` package group:
`100000000.img` is merged from `Map1_000.wz` and keeps its true source package
identity. Inspecting `Data/Map/Map/Map1/Map1_000.wz` selector
`100000000.img` with debug metadata resolves `miniMap/_outlink` to image
selector `100000000.img` and value path `miniMap/canvas`. A later smoke
extension validates `Data/Character/Character_000.wz` selector `00002000.img`:
`walk1/0/body/_outlink` resolves through `Data/Character/_Canvas/_Canvas_000.wz`
and the Canvas preview service can load the linked direct-zlib Canvas pixels.
The same extension validates `Data/Sound/Sound_000.wz` selector
`AchievementEff.img`: `GradeUp` exposes Sound_DX8 duration, data offset, and
data length metadata, while retaining the stable
`wcx.payload.audio.unsupported` diagnostic for not-yet-decoded audio payloads.
These tests read local files only and do not commit client data.

A later M5 local inventory scan found 780 `.wz` files under the local GMS
`Data` directory; `headers` did not report any `PKG2`, `unknown`, or invalid WZ
headers in that scan. The same inventory found no `List.wz` and no `.mn` files,
but did find 10 `.ms` pack files under `Data/Packs`, including Mob and Skill
packs. That makes `.ms` container inspection sample-driven for this client.
`List.wz`, `.mn`, and PKG2 are not covered by the current local GMS sample, but
this does not remove them from the compatibility target: older clients may still
require them. `.mn` keeps synthetic coverage through WC's shared MS loader path.
`List.wz` now has a first parser slice based on WC's `Wz_Crypto.LoadListWz`:
it decodes no-op/KMS/GMS string-list records, excludes the `dummy` sentinel, and
projects the decoded list through `inspect` as `format: listwz`. This is still
only the observable helper-file slice; it is not yet wired into PKG1 string
key/profile detection, and real `List.wz` smoke still waits for an older-client
sample. PKG2 still needs WC reference behavior or a representative sample
before directory parser behavior is accepted.

WCX now inspects `.ms` and `.mn` v2/Snow and v4/ChaCha20 container directory
tables through the normal `inspect` path. The directory slice reads header
metadata, entry names, checksums, flags, relative blocks, absolute offsets,
sizes, and unknown fields, then projects slash-separated entry names into the
Core inspection tree as image nodes. The image-payload slice now follows WC's
`Ms_Image.OpenRead()` / `Ms_ImageV2.OpenRead()` model: v2/Snow payloads decrypt
with one continuous Snow pass plus a second pass over the first 1024 bytes,
and v4/ChaCha20 payloads decrypt the first 1024 bytes while leaving the
remaining bytes raw. The decrypted stream then enters the existing IMG
inspection path. A local GMS smoke run verified all 10 `Data/Packs/*.ms` files
inspect successfully as directory tables and `Packs/Skill_00002.ms` ->
`Skill/15500.img` extracts as a `Property` IMG without committing client data;
`.mn` is currently fixture-covered only because the local client has no `.mn`
sample.

Canvas preview now uses the same MS/MN image payload path as `inspect`. Synthetic
coverage verifies direct MS Canvas payloads and same-container MS `_outlink`
resolution. A local GMS smoke run also verified `Packs/Mob_00000.ms` selector
`Mob/1150000.img`, value `move/0/_outlink`: WCX resolves the logical target
`Mob/_Canvas/1150000.img/move/0` through the workspace, which can land on the
current MS container or on the matching WZ package group such as
`Data/Mob/_Canvas/_Canvas_000.wz`, matching WC's global `FindWz(path)` style.

M5 scalar fixtures now explicitly cover the supported binary IMG scalar
property encodings: null, both int16 property tags, compressed and expanded
int32, alternate int32 tag, compressed and expanded int64, compressed and
expanded single, double, string, and empty nested `Property` nodes. The same
fixture family is covered through WzLib reader tests and through CLI
`inspect --debug` output so the supported scalar surface is observable before
future compare/search work depends on it.

## Milestone 2 Parser Coverage

M2 accepted the following migrated behavior as complete:

- PKG1 and PKG2 package header detection.
- PKG1 directory enumeration and recursive directory entries.
- PKG1 name decoding, including string-reference names.
- WZ version, hash version, and hash-offset calculation.
- Directory string-key modes: none, KMS, GMS, and auto.
- IMG root object type detection.
- Bounded `Property` traversal with scalar and nested property values.
- Vector, Convex2D, UOL, Canvas, RawData, Video, and Sound metadata.
- Lua IMG inspection.
- WC text-format IMG v1/v2 inspection.

The following work moved beyond M2:

- PNG export.
- Full Canvas pixel decode matrix.
- Audio and video payload decoding.
- Full PKG2 directory parsing.
- UI browsing.

Read-only local smoke verification covered `Base/Base.wz`, `Base/Base_000.wz`,
`UI/UI_000.wz`, and `Sound/Sound_000.wz`. `UI/WZ2Lua/WZ2Lua.wz` was verified
as a directory-only smoke sample; the sampled local GMS package did not expose
a direct `.lua` image through the supported inspection path. Lua IMG and
text-format IMG parser behavior is currently locked by committed synthetic
fixtures, and still needs direct real-sample verification when a suitable local
entry is found.
