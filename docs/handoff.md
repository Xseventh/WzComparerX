# WCX Handoff

This file lets a new Codex conversation resume the project without access to the
original chat.

## Repository

Path used in the previous Codex thread:

```text
/Users/seventh/Documents/Codex/2026-04-26/https-github-com-kagamia-wzcomparerr2-https/WzComparerX
```

Current development branch:

```text
codex/wcx-modernization
```

Current remote layout:

```text
origin   Xseventh/WzComparerX
upstream Kagamia/WzComparerX
```

Recent commits:

```text
65a8674 Split Canvas image context helpers
5c4b080 Align Canvas export with preview service
63c5681 Resolve UOL canvas preview links
35a16b2 Move Canvas bitmap decode into WzLib
7a4f18d Lock export missing image diagnostic
5e1adf0 Add image entry not found diagnostic
1f3e2db Record Lua text sample scan
ea6ccc8 Add real Video metadata smoke
6d50d36 Guard linked image identity
31bfc34 Add real Convex2D smoke coverage
e95e094 Extract inspection value projection
2185d4c Add UI RawData smoke coverage
e580ba9 Add real Vector smoke coverage
7e424ed Add UI outlink App smoke
9872b75 Add UI canvas outlink smoke
aa53306 Refresh handoff recent commits
c4df81d Resolve package group image selectors
da76f30 Use full viewport for Canvas preview auto scale
247060a Make Canvas preview scale fixed
cebcf93 Fit Canvas preview auto scale to viewport
662bccf Preserve MN container kind
fbb8b7c Add MS outlink App smoke
789828e Skip List.wz in folder inspection
9f253de Preview RGB565 scale 16 canvases
fd1b8c9 Preview BC7 zlib canvases
cd91689 Preview remaining direct zlib canvas formats
c1ecd07 Preview RGBA1010102 zlib canvases
b91152a Preview DXT3 zlib canvases
1ff6fb1 Preview DXT5 zlib canvases
307e83c Preview 16-bit zlib canvases
c0c5a00 Lock PKG1 directory edge fixtures
9087c4f Lock split package diagnostic boundary
a59e4dd Use singular external client data variable
3f75f77 Lock external client path parsing
01f85ea Keep one external client smoke variable
b9db110 Unify external client smoke entry
7bb810e Defer optional KMS smoke tests
9288f2e Add optional KMS PKG2 smoke tests
a107b16 Lock modern PKG2 CLI diagnostics
5c6449a Use null modern PKG2 WZ version
800b555 Add modern PKG2 header support
1e67d01 Support PKG2 KMST1200 directory inspection
9299cf0 Expand GMS parser smoke coverage
8647e80 Expand GMS IMG smoke coverage
129ae95 Update handoff after IMG scalar fixtures
6d55a2c Lock IMG scalar inspection fixtures
b30a659 Update handoff after List.wz inspection
d5ab55e Inspect List.wz string lists
2c0286a Update handoff after MS outlink preview
d7a5918 Resolve MS outlink canvas previews
29bcf79 Refresh handoff recent commits
```

## What Has Been Done

- Cloned WC and WCX.
- Inspected WC's architecture, complexity, and maintenance state.
- Established that WCX should not be a direct WC UI port.
- Installed/verified local tooling:
  - .NET SDK `10.0.203`
  - ripgrep `15.1.0`
  - ffmpeg `8.1_1`
  - ImageMagick `7.1.2-21`
- Added `/usr/local/share/dotnet` to `~/.zshrc`.
- Created `.NET 10` WCX solution skeleton.
- Added Avalonia MVVM app shell.
- Added CLI, Core, Domain, Rendering, WzLib, and test projects.
- Added development docs, ADRs, roadmap, command reference, and bootstrap log.
- Verified build and tests after bootstrap.
- Implemented Milestone 1 headless resource browser:
  - synthetic raw node fixture and reader,
  - Core document/workspace/list services,
  - CLI `list` command,
  - deterministic tests.
- Started Milestone 2 real parser migration:
  - WZ `PKG1`/`PKG2` header detection,
  - CLI `header` and recursive `headers` commands,
  - JSON output for `list` and header workflows,
  - PKG1 top-level directory inspection,
  - PKG1 directory-name decoding with no-op/KMS/GMS key modes,
  - PKG1 version hash and entry offset calculation,
  - PKG1 `0x02` string-reference name decoding,
  - PKG1 recursive directory-table pre-read and inspection enumeration,
  - minimal IMG payload inspection for top-level object type,
  - bounded IMG `Property` inspection for first-layer scalar values and nested
    `Property` objects,
  - IMG Vector, Convex2D, UOL, Canvas metadata, RawData metadata, and
    Canvas#Video metadata value inspection,
  - Canvas payload compression metadata and uncompressed size estimates,
  - IMG Sound_DX8 metadata value inspection,
  - string-key auto-detection across no-op/KMS/GMS modes,
  - top-level IMG object value inspection for supported non-`Property` object
    types,
  - Lua image block inspection for `.lua` entries,
  - text-format IMG v1/v2 property inspection,
  - initial Core `inspect` model and CLI command,
  - `inspect --debug` with structured directory and IMG diagnostics,
  - CLI-level golden tests for representative `inspect` and `inspect --debug`
    text/JSON output,
  - initial Core export abstraction and CLI `export` command for metadata, WC
    text-format IMG streams, and Lua IMG scripts,
  - export `--out` file output, byte-oriented export documents, Lua multi-block
    concatenation, and structured export diagnostics for unsupported exports,
  - split Lua, text IMG, and binary IMG inspection readers out of
    `WzImageInspectionReader`,
  - stable diagnostic codes/sources for parser payload limitations and export
    conditions.

## Important Decisions

- Use .NET 10.
- Use Avalonia for the initial desktop UI shell.
- Build headless core and CLI before feature-heavy UI.
- Treat WC as a reference implementation, not a compatibility target.
- Preserve WC's format knowledge through tests before refactoring.
- `inspect` is the long-term resource observation surface.
- `inspect --debug` is the development diagnostic surface for low-level parser
  metadata.

## Current State

Milestones 1 through 4 are complete. Milestone 5 Parser Coverage And Resource
Model Baseline is active. M4 reused Core inspection/export models and kept
parser behavior out of App view models.
The next risk is building Compare/Search/UI/export features on top of parser and
resource-identity contracts that are still too narrow. M5 uses
`docs/parser-coverage-matrix.md` as the working parser/resource-model checklist
for choosing the next slices. M2 delivered real WZ
package header detection, PKG1 directory inspection, recursive directory
entries, string-key handling, IMG object and property metadata, Lua/text IMG
inspection, and payload metadata for Canvas, RawData, Video, and Sound values.

`inspect` now projects synthetic fixtures, WZ directories, and WZ IMG payloads
into a generic inspection tree so future UI/export/search work does not depend
directly on parser DTOs. `inspect --debug` exposes the low-level fields that
were useful during parser migration, including PKG1 node types, sizes,
checksums, hash offsets, calculated offsets, selected string key, WZ/hash
version, selected IMG entry metadata, object type, object value metadata,
property type/kind metadata, and Canvas/RawData/Video/Sound payload offsets and
lengths. `--key auto` selects among no-op/KMS/GMS directory string decoding.
Selected IMG payloads are lazily loaded as complete single-IMG inspections by
default, matching WC. `--depth` remains available as a CLI diagnostics limiter;
`--depth 0` prints only the top-level IMG object type.

The old temporary parser/CLI terminology has been retired from active design,
command documentation, file names, and active tests. The supported observation
surface is now `inspect` and `inspect --debug`.

Canvas pixel decoding now uses a shared WzLib Canvas bitmap decoder for Preview
and CLI Canvas export. It converts direct-zlib `ARGB4444` (`1`), `ARGB1555`
(`257`), `RGB565` (`513`), `R16` (`769`), `ARGB8888` (`2`), `A8` (`2304`),
`RGBA1010102` (`2562`), `DXT3` (`1026`), `DXT5` (`2050`), `DXT1` (`4097`),
`BC7` (`4098`), and `RGBA32Float` (`4100`) Canvas payloads to BGRA8888 bytes.
`RGB565` also handles WC's `scale=4` / `ActualScale=16` path by repeating each
source pixel into a 16x16 block. PNG export and non-direct-zlib Canvas payloads
remain later work.
Canvas selection and Canvas link resolution now also share one Core service
between Avalonia Preview and CLI Canvas export, so `.ms` / `.mn` payloads,
package-group selectors, `_inlink` / `_outlink` / `source` links, and UOL-linked
Canvas values use the same resolver in both surfaces.
RawData/Sound payload decoding and Canvas#Video frame decoding/playback are not
implemented yet. Canvas#Video `MCV0` header/table parsing is implemented and
exposes fourCC, dimensions, frame count, alpha/timing flags, and first-frame
offset metadata through `inspect --debug`. `external/VPDecoder` is now tracked
as a submodule and has been validated as a managed raw `VP90` packet decoder
candidate for the current local GMS first color/alpha frame chunks. The pinned
submodule revision exposes memory-first `ReadOnlySpan<byte>` /
`ReadOnlyMemory<byte>` decode APIs, `DecodeFrameWithAlpha`, and `Reset()`, but
it has not yet been wired into Core/App/CLI decode surfaces.
`WzComparerX.Rendering` now has `WzImageVideoFrameDecoder`, which reads selected
color/alpha chunks from an image payload stream using `WzImageVideoInspection`
metadata and delegates raw `VP90` decode to `Vp9RawVideoPacketDecoder`. The
pinned VPDecoder revision documents VP9 sequence semantics: one decoder instance
is one stream state; `DecodeFrameWithAlpha` is a single-frame convenience helper,
not a full color+alpha sequence decoder. Future playback should maintain
separate color and alpha decoder states.
Lua image entries report script length and a short UTF-8 snippet; `export --type lua`
writes the full decoded script for supported Lua IMG blocks. Text-format IMG streams starting with `#Property` or
`Root <Property>` inspect as bounded `Property` trees and can be exported with
`export --type text`. Text exports write to stdout by default or exact bytes to
`--out <path>`; Canvas export writes BGRA8888 bytes, is binary-only, and
requires `--out`. Diagnostics carry stable severities, codes,
sources, and resource paths through `ResourceInspectionDiagnostics`; CLI text
output prints codes in brackets when present and avoids volatile raw exception
text. The diagnostic rules are documented in `docs/diagnostics.md`.
`WzImageInspectionReader` is now the small image-entry dispatcher, and
`WzImageBinaryInspectionReader` is the binary IMG entry coordinator. Binary IMG
object/property parsing lives in `WzImageBinaryObjectInspectionReader`, with
bounded property traversal in `WzImageBinaryPropertyInspectionReader` and IMG
string decoding in `WzImageBinaryStringReader`. Text and Lua stream shapes are
in their own readers. Canvas/RawData/Video/Sound payload metadata lives in
`WzImagePayloadInspectionReader`, and shared IMG binary read primitives live in
`WzImageBinaryReaderPrimitives`; the next parser split should target additional
object-type families only when new behavior needs them.

Canvas decode/export follows the narrow plan in
`docs/canvas-decode-export-plan.md`: synthetic fixture first, direct zlib and a
single verified pixel format first, payload decoder separate from parser
metadata and Core export. The current `export --type canvas --out` slice reuses
the same Core Canvas image service and shared WzLib BGRA8888 decoder that
Preview uses. PNG export is not required for M3; it remains later user-facing
image export work. Canvas export now uses `--value <property-path>` for
IMG-internal Canvas selection; the selector design is in
`docs/canvas-export-selector-plan.md`.
Committed synthetic PKG1 hex fixtures now cover Canvas, WC text-format IMG, and
Lua IMG export paths, with expected text/JSON/stdout/stderr golden outputs
under `fixtures/expected`.

Recent CLI boundary cleanup:

- `export --json` now returns a usage error because export writes resource
  content directly; metadata export is already JSON.
- The current string-key CLI surface is `auto|none|noop|kms|gms`. The no-op mode
  should not be presented as a service-region key.

M3 closeout:

- `inspect`, `inspect --debug`, and `export` are the headless automation
  surfaces.
- Export covers metadata JSON, WC text-format IMG, Lua IMG, and direct-zlib
  Canvas BGRA8888 bytes.
- Diagnostics have stable severities, sources, codes, CLI text formatting, and
  docs.
- XML dump, PNG export, broader Canvas decode, audio/video decode, and full PKG2
  directory parsing are later work.

M4 completed:

- The Avalonia shell now has a path-based resource loader.
- The main window view model calls `ResourceInspectionService` and projects Core
  inspection nodes into a UI tree.
- The UI has an IMG selector field plus a `Load IMG` action for manually entered
  IMG selectors or refreshes. String key input mirrors the current inspect
  workflow. Resources and IMG Content are now separate trees: selecting an image
  node extracts that single IMG into IMG Content while the package/directory
  Resources tree stays in place. This matches WC's `Wz_Image.TryExtract()`
  browsing model without replacing the source tree.
- The path row has one `Browse` entry with native file and folder picker menu
  choices. The `Load` action automatically handles file paths and folder paths.
  Folder paths scan `.wz` headers into a folder inspection tree; selecting a
  package node can still be activated from the tree. Opening a new file, folder,
  or package clears previous IMG Content and Canvas preview state.
- Double-clicking activatable resource nodes routes through the ViewModel:
  package nodes open their package, and image nodes load/refresh IMG Content.
- Image nodes from linked or merged split packages now resolve through their
  actual `<package>.wz/<image>.img` target, so IMG Content can load from the
  original shard while the current Resources tree remains focused on the entry
  package.
- The UI shows document metadata, selected-node metadata, and selected
  diagnostics.
- The UI has a Preview tab for Canvas values. Preview follows IMG Content
  selection: selecting a Canvas node previews that exact value, root Canvas IMG
  objects use the same path, UOL nodes resolve their relative target and then
  follow any Canvas `source` / `_inlink` / `_outlink` child link in WC order,
  and `source` / `_inlink` / `_outlink` string nodes resolve to linked Canvas
  values when the workspace path can be mapped. The
  preview path uses the current direct-zlib `1` / `257` / `513` / `769` / `2`
  / `2304` / `2562` / `1026` / `2050` / `4097` / `4098` / `4100` viewer slices through
  `ResourceCanvasImageService`. Auto display scale enlarges small bitmaps with
  capped integer scaling and shrinks very large bitmaps proportionally, while
  manual `1x`, `2x`, `4x`, `8x`, and
  `16x` buttons remain
  exact.
- The UI has a basic activity log for load, inspection, and error events.
- Core directory inspection now has a WC-style package group layer above the
  single-file WzLib parser. Opening `Name.wz` detects `Name.ini` and
  `LastWzIndex`, falls back to contiguous `Name_000.wz...` enumeration when the
  ini is absent, and merges shard directory entries under the entry package
  while retaining each shard IMG's original source path.
- UI command state now avoids redundant re-opening of the current package.
- App key/options text parsing is isolated from `MainWindowViewModel`, keeping the
  view model focused on orchestration and visible state.
- App projection helpers are now split out of `MainWindowViewModel`: selector
  normalization lives in App services, while resource node, metadata,
  diagnostics, and activity log projection models live in dedicated ViewModel
  files.
- `WzComparerX.App.Tests` now owns Avalonia UI/ViewModel tests and uses
  Avalonia Headless for window/control smoke coverage. Skia-backed headless
  screenshot smoke tests verify that the main window renders nonblank content
  and keeps primary controls inside default and compact viewports. Headless
  coverage now also asserts invalid path, folder open, package open, and image
  inspection visible states. Core.Tests no longer references the App project.
- Core inspection now performs conservative split-package linking for empty
  PKG1 directory stubs at any depth when the stub has no parsed children. For
  GMS-style layouts such as `Base/Base.wz`, sibling packages like
  `Effect/Effect.wz` and `Effect/Effect_000.wz` are grafted under the `Effect`
  directory node. Same-package subtree links such as `UI/UI.wz` to
  `UI/_Canvas/_Canvas.wz` are also resolved. Broader workspace-relative lookup
  is limited to the `Base/Base.wz` index shape; linked packages resolve nested
  stubs relative to their own package directory. Split-package directory trees
  now load eagerly in the WC style; `--depth` only controls IMG/property
  inspection.
- Richer task/progress handling is still pending.
- Canvas Preview workflow logic has started moving out of
  `MainWindowViewModel`: node eligibility, value-selector selection, and Core
  Canvas image service calls now live in an App workflow helper. The view model
  still owns visible state, request ordering, diagnostics projection, and
  activity messages.
- IMG Content workflow logic has also started moving out of
  `MainWindowViewModel`: selected/manual selector resolution, current-target
  checks, and Core inspect calls now live in an App workflow helper. The view
  model still owns visible tree state, request ordering, and activity messages.
  Manual selectors now flow through the Core package-group image loader, so
  `Load IMG`, `inspect <entry.wz> <Image.img>`, Canvas preview, and Canvas
  export can resolve numbered-shard IMG payloads from the entry package path.
- Resource detail projection for document metadata, selected-node metadata, and
  selected diagnostics now lives in a small ViewModel helper, keeping panel
  formatting rules out of the main window orchestration.
- Core split-package link path resolution now lives in a dedicated helper. The
  inspection service still composes the resource tree, but Base workspace
  fallback, same-package relative lookup, candidate de-duplication, and ancestor
  path normalization no longer sit directly in the main inspection flow.
- Core IMG value metadata and payload diagnostic projection now lives in a
  dedicated helper, so `ResourceInspectionService` composes inspection trees and
  identities without directly owning every Canvas / RawData / Video / Sound /
  Lua / text / geometry value family switch.
- Core inspection nodes now carry `ResourceInspectionIdentity` with package
  path, image selector, and inside-IMG value path fields. Merged shard image
  identities point to their true source shard package. The current identity
  contract is documented in `docs/resource-identity.md`.
- Core image inspection now falls back from an entry package to its loaded
  numbered shards when the requested selector is not present in the entry
  package. The returned inspection identity and source path still point at the
  true shard package, which keeps resource identity and export streams aligned.
- Link-like IMG values (`source`, `_inlink`, `_outlink`, `link`, and UOL) now
  populate normalized `Identity.LinkedTarget` and debug `linkKind` /
  `linkedTarget` metadata. `inspect --debug` can also populate
  `Identity.ResolvedLinkedTarget` plus resolved-link debug metadata for local
  `_inlink`, relative UOL, and logical `source` / `_outlink` / `link` targets
  that can be resolved through the current `Data` workspace. Canvas preview
  uses the same resolver for UOL and link-string selections, including the
  common WC chain where a frame UOL points at a placeholder Canvas whose child
  `_outlink` points at the real Canvas. It emits
  `wcx.viewer.canvas.linkUnresolved` when a selected UOL / `source` /
  `_inlink` / `_outlink` cannot be resolved to a Canvas value.
- Failed split-package candidates now emit `wcx.package.link.unresolved` on the
  directory stub. Missing candidates remain silent so ordinary empty directory
  stubs do not become noisy diagnostics.
- Package groups created from `.ini` / `LastWzIndex` now report stable warning
  diagnostics on the package root when a declared numbered shard is missing or
  cannot be loaded: `wcx.package.group.shardMissing` and
  `wcx.package.group.shardInvalid`.
- Missing IMG selectors now emit the stable inspection diagnostic
  `wcx.inspection.image.notFound` through CLI/Core/App surfaces instead of
  leaking raw `Image entry not found` exception text.
- PKG2 has KMST1199/1200 and modern KMS directory/profile/offset slices.
  Synthetic `pkg2_kmst1200` and modern KMS fixtures cover directory entries,
  CLI output, and image payload inspection. Modern KMS 0x44-byte envelope
  samples report `pkg2HeaderVariant: modern`, `formatProfile:
  pkg2_modern_kms`, and no synthetic `wzVersion`; unsupported PKG2 profiles
  still return the stable parser diagnostic
  `wcx.package.pkg2.directoryUnsupported`.
- PKG1 directory inspection now has deterministic malformed-table guards for
  negative directory entry counts and `0x02` string-reference names that resolve
  beyond the file.

Local GMS smoke status:

- Base, Base_000, UI_000, Sound_000, and WZ2Lua directory-only smokes are
  recorded in the M2 closeout log.
- M4 real GMS UI smoke is recorded in
  `docs/logs/2026-05-01-m4-gms-ui-smoke.md`. The local run opened
  `Map/Map/Map1/Map1.wz`, merged `Map1_000.wz` entries through the package group
  path, selected `100000000.img`, populated IMG Content, resolved
  `miniMap/_outlink` to the real Canvas preview, and covered large directory /
  large Canvas auto-scaling behavior. Screenshots were saved under
  `/private/tmp/wcx-gms-ui-smoke-2026-05-01/` during the run and are not
  committed.
- M5 optional Core smoke tests now use a unified external-client entrypoint:
  `WCX_CLIENT_DATA_DIR` points at a client `Data` directory. The current tests
  validate whichever supported file families are discovered,
  including representative package roots, `Map1.wz` package-group image
  identity, and `Map1_000.wz/100000000.img` `miniMap/_outlink` resolved target
  identity for the local GMS layout. The original GMS smoke run is recorded in
  `docs/logs/2026-05-02-gms-core-smoke-tests.md`.
  The `.ms` container extension is recorded in
  `docs/logs/2026-05-03-ms-container-directory-inspection.md`; the optional
  smoke now also validates local `Data/Packs/*.ms` directory tables.
- A local GMS M5 inventory found 780 `.wz` files, all PKG1 by `headers` scan;
  no local `List.wz`, `.mn`, or PKG2 WZ sample was found, but 10
  `Data/Packs/*.ms` files exist. `.ms` / `.mn` v2/Snow and v4/ChaCha20
  container directory tables now inspect through Core, folder inspection lists
  `.ms` / `.mn` packages alongside `.wz`, `inspect` / folder output preserves
  the actual container kind (`ms` or `mn`) even though both share the same WC
  loader path, and `.ms` / `.mn` image payloads can be extracted into the
  existing IMG inspection path. Optional GMS smoke covers all local
  `Data/Packs/*.ms` directory tables and verifies
  `Packs/Skill_00002.ms` -> `Skill/15500.img` extraction; synthetic `.mn`
  fixtures lock the same WC loader path while real `.mn` smoke waits for an
  older-client sample.
  `List.wz` now has a first-slice WzLib reader and Core `inspect` projection
  for no-op/KMS/GMS string-list records. It remains a helper file rather than a
  package tree; folder inspection skips it rather than surfacing it as an
  invalid WZ package. It is not yet wired into PKG1 string key/profile
  selection, and still needs real older-client smoke because the current local
  GMS install lacks a sample.
- A user-supplied local KMS/KMST-style PKG2 sample at `~/Downloads/Item_000.wz`
  is used for manual smoke only and is not committed. WCX detects it as
  `pkg2_kmst1200` with `hashVersion = 0xb0da16f2`, lists root images
  `ItemOption.img`, `ItemSellPriceStandard.img`, `SkillOption.img`, and
  `ThothSearchOption.img`, and can inspect `SkillOption.img` through the shared
  IMG reader to top-level `skill`, `socket`, and `inc` objects.
- A user-supplied local modern KMS PKG2 sample set under `~/Downloads/new_kms/`
  is also smoke-only and is not committed. WCX detects `Item_000.wz` and
  `String_000.wz` as `pkg2_modern_kms`, lists four and 26 root IMG entries
  respectively, and confirms `ItemSellPriceStandard.img` / `Eqp.img` flow into
  the shared IMG reader. TODO: add KMS coverage to the same external-client
  smoke harness after a complete KMS client is available, rather than relying on
  this small ad-hoc sample set.
- `wcx.package.ms.imageUnsupported` now means an `.ms` / `.mn` image entry was
  found but the implemented v2/v4 payload extraction or downstream IMG reader
  could not inspect it; unsupported container shapes still return
  `wcx.package.ms.directoryUnsupported`.
- Canvas preview now uses the same MS/MN image payload extraction path as
  `inspect`. Synthetic tests cover direct MS Canvas preview and same-container
  MS `_outlink` resolution; optional local GMS smoke covers
  `Packs/Mob_00000.ms` selector `Mob/1150000.img`, value
  `move/0/_outlink`, which resolves the logical Canvas target through the
  current workspace. A matching Avalonia Headless smoke now opens the same
  `.ms` container, selects the IMG, selects the `_outlink`, and verifies the
  Preview tab renders the linked Canvas without an App-layer "Invalid WZ
  package" regression.
- Optional local GMS smoke also covers deeper WZ IMG families:
  `Character/Character_000.wz` selector `00002000.img` resolves
  `walk1/0/body/_outlink` through `Character/_Canvas/_Canvas_000.wz` and loads
  the linked Canvas preview pixels; `Sound/Sound_000.wz` selector
  `AchievementEff.img` exposes `GradeUp` Sound_DX8 metadata and retains the
  stable `wcx.payload.audio.unsupported` diagnostic.
- A later optional local GMS smoke expansion covers
  `Effect/Effect_000.wz` selector `BasicEff.img`, resolving
  `scout/back/0/_outlink` through `Effect/_Canvas/_Canvas_002.wz` and loading
  linked Canvas preview pixels. It also records future StringLinker/domain input
  shapes: `String/String_000.wz` selector `Eqp.img` exposes `Eqp/Cap`,
  `Eqp/Weapon`, and `Eqp/Accessory`, while `Item/Item_000.wz` selector
  `SkillOption.img` exposes `skill`, `socket`, `inc`, and representative
  `skillId` / `reqLevel` int32 values.
- Optional local GMS UI link smoke now covers `UI/UI_000.wz` selector
  `Basic.img`: `Cursor/0/0/_outlink` resolves through
  `UI/_Canvas/_Canvas_000.wz` and the Canvas preview service loads the linked
  24x28 format-1 direct-zlib pixels. A matching Avalonia Headless smoke opens
  `UI_000.wz`, selects `Basic.img`, selects the `_outlink` from IMG Content,
  and verifies the Preview tab renders the linked Canvas without App-layer link
  resolution drift.
- Optional local GMS UI RawData smoke covers `UI/UI_000.wz` selector
  `Login.img`, path `ClassSelect/back/1/110/skeleton.skel`, preserving RawData
  version, data length, data offset, identity value path, and the stable
  `wcx.payload.rawData.unsupported` info diagnostic.
- Optional local GMS UI Canvas#Video smoke covers `UI/UI_000.wz` selector
  `UIGachapon.img`, path `royalStyle/openvideo/intro`, preserving video
  metadata, `MCV0` header metadata, identity value path, and the stable
  `wcx.payload.video.unsupported` info diagnostic. Optional local Packs smoke
  also covers `Packs/Mob_00002.ms` selector
  `Mob/BossPattern/BossFirstAdversary.img`, path `1069/003/effect/0`, with
  `VP90`, dimensions, frame count, alpha-map, and first-frame offsets.
- Optional local GMS Vector smoke now covers
  `Character/Character_000.wz` selector `00002000.img`
  (`walk1/0/body/origin`, `walk1/0/arm/origin`) and
  `Packs/Mob_00000.ms` selector `Mob/1150000.img`
  (`move/0/origin`, `lt`, `rb`, `head`). Convex2D smoke covers
  `UI/UI_000.wz` selector `RunnerGame.img`, path
  `RunnerGameUI/Object/0/Tile/0/foothold`, with four polygon points.
- Lua IMG and WC text-format IMG behavior is locked by synthetic fixtures, but
  still needs direct real-sample smoke verification when a suitable local
  client entry is found. A parser-based local GMS scan across common package
  families found no `.lua` image entries; text-format discovery remains
  deferred because the supported signatures are payload-level rather than
  directory-name-level.

## Suggested Next Prompt

Use this in a new Codex project conversation:

```text
We are continuing the WCX modernization project in this repository. Please read
AGENTS.md, docs/README.md, docs/handoff.md, docs/roadmap.md, and
docs/development-guidelines.md first. Continue active Milestone 5 Parser
Coverage And Resource Model Baseline work. Reuse Core inspection/export models,
keep parsing out of view models, use docs/parser-coverage-matrix.md to choose
parser/resource-model/real-client-smoke slices, add focused tests where
practical, run build/test, update docs/logs, and commit the work on the current
branch.
```

## Caution

Do not push to any remote unless the user explicitly asks. In the current local
clone, `origin` points to `Xseventh/WzComparerX` and `upstream` points to
`Kagamia/WzComparerX`.
