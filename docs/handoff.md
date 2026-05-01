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
35a8928 Record GMS container inventory
3b7bdb0 Add optional GMS core smoke tests
27b9a27 Guard malformed PKG1 directories
aa093ce Document headless UI test convention
6330ff0 Report package group shard diagnostics
dda5994 Resolve linked target identity
3ce6b97 Document resource identity contract
d426cf6 Lock media payload diagnostics
4dcccee Report PKG2 inspection blocker
f21ec65 Report unresolved Canvas preview links
38452b2 Report unresolved split package links
777a090 Add Core link target identity
03a9199 Add Core resource inspection identity
487d82d Add M5 parser coverage matrix
aba5c63 Replan post-M4 roadmap
97b584e Split Core split package resolver
c502e80 Split Avalonia details projection
171b99c Split Avalonia image content workflow
ca589a9 Close M4 and split Canvas preview workflow
bb67eff Record M4 GMS UI smoke
8d5e68d Align inspection defaults with full IMG browsing
251ddcc Simplify Avalonia IMG controls
ecfd10a Align Avalonia IMG browsing with WC
9725296 Shrink large Canvas previews in auto mode
3208f86 Add Canvas preview zoom controls
7310f1d Preview Canvas on image selection
5abfc4f Fix linked package image inspection
70baa8d Add Avalonia Canvas preview
84c6c0c Align IMG inspection with WC lazy loading
1f13770 Document WCX project progress in Chinese
335835a Eager load split package trees
1949d1b Match WC split package lookup roots
39a4b4e Bound recursive split package expansion
2aeab66 Generalize split package linking
b0b332e Link split packages in resource inspection
be4a713 Add Avalonia package return navigation
cc9b602 Support manual image inspection in Avalonia
19d009f Split Avalonia resource view models
ecfd7b2 Harden Avalonia UI state tests
d6607fe Test Avalonia image node activation
c47b3c1 Add Avalonia tree click smoke test
7975530 Add Avalonia tree selection smoke test
2e64dc7 Expand Avalonia resource root by default
24f6525 Show Avalonia activity log panel
aa7ccaa Save optional Avalonia headless screenshots
d7301c6 Add Avalonia headless screenshot tests
68fccec Add Avalonia headless app tests
2299e11 Make Avalonia activity log visible
f6337a2 Add Avalonia activity log panel
6ee6782 Add Avalonia resource node activation
c1ef46a Add folder choice to Avalonia browse menu
abbb766 Align Avalonia open entry with WC
5e201c0 Add Avalonia folder package browser
29e560c Add Avalonia resource file picker
1ef8fab Normalize Avalonia image selectors
cae2482 Add Avalonia image inspection workflow
0bccdaa Add basic Avalonia resource browser
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

Milestones 1 through 4 are complete. The current mainline is post-M4 cleanup
and Milestone 5 Parser Coverage And Resource Model Baseline planning. M4 reused
Core inspection/export models and kept parser behavior out of App view models.
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

Canvas pixel decoding has the first narrow direct-zlib raw-byte slice for
format `2` / `2562`; PNG export and broader Canvas format coverage remain
later work. RawData/Video/Sound payload decoding is not implemented yet. Lua image
entries report script length and a short UTF-8 snippet; `export --type lua`
writes the full decoded script for supported Lua IMG blocks. Text-format IMG
streams starting with `#Property` or `Root <Property>` inspect as bounded
`Property` trees and can be exported with `export --type text`. Text exports
write to stdout by default or exact bytes to `--out <path>`; Canvas export is
binary-only and requires `--out`. Diagnostics carry stable severities, codes,
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
metadata and Core export. The first raw-byte `export --type canvas --out` slice
exists for direct zlib Canvas payloads with format `2` / `2562`. PNG export is
not required for M3; it remains later user-facing image export work. Canvas
export now uses `--value <property-path>` for IMG-internal Canvas selection; the
selector design is in `docs/canvas-export-selector-plan.md`.
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
- Export covers metadata JSON, WC text-format IMG, Lua IMG, and raw direct-zlib
  Canvas bytes.
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
  objects use the same path, and `source` / `_inlink` / `_outlink` string nodes
  resolve to linked Canvas values when the workspace path can be mapped. The
  preview path uses the current narrow direct-zlib format `1` / `2` slice
  through `ResourceCanvasImageService`. Auto display scale enlarges small
  bitmaps with capped integer scaling and shrinks very large bitmaps
  proportionally, while manual `1x`, `2x`, `4x`, `8x`, and `16x` buttons remain
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
- Resource detail projection for document metadata, selected-node metadata, and
  selected diagnostics now lives in a small ViewModel helper, keeping panel
  formatting rules out of the main window orchestration.
- Core split-package link path resolution now lives in a dedicated helper. The
  inspection service still composes the resource tree, but Base workspace
  fallback, same-package relative lookup, candidate de-duplication, and ancestor
  path normalization no longer sit directly in the main inspection flow.
- Core inspection nodes now carry `ResourceInspectionIdentity` with package
  path, image selector, and inside-IMG value path fields. Merged shard image
  identities point to their true source shard package. The current identity
  contract is documented in `docs/resource-identity.md`.
- Link-like IMG values (`source`, `_inlink`, `_outlink`, `link`, and UOL) now
  populate normalized `Identity.LinkedTarget` and debug `linkKind` /
  `linkedTarget` metadata. `inspect --debug` can also populate
  `Identity.ResolvedLinkedTarget` plus resolved-link debug metadata for local
  `_inlink`, relative UOL, and logical `source` / `_outlink` / `link` targets
  that can be resolved through the current `Data` workspace. Canvas preview
  uses the same resolver and emits `wcx.viewer.canvas.linkUnresolved` when a
  selected `source` / `_inlink` / `_outlink` cannot be resolved to a Canvas
  value.
- Failed split-package candidates now emit `wcx.package.link.unresolved` on the
  directory stub. Missing candidates remain silent so ordinary empty directory
  stubs do not become noisy diagnostics.
- Package groups created from `.ini` / `LastWzIndex` now report stable warning
  diagnostics on the package root when a declared numbered shard is missing or
  cannot be loaded: `wcx.package.group.shardMissing` and
  `wcx.package.group.shardInvalid`.
- PKG2 remains header-only, but `inspect` now returns a stable parser error
  diagnostic, `wcx.package.pkg2.directoryUnsupported`, and exits non-zero
  instead of exposing unsupported directory parsing as a raw exception.
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
- M5 optional Core GMS smoke tests are recorded in
  `docs/logs/2026-05-02-gms-core-smoke-tests.md`. When `WCX_GMS_DATA_DIR`
  points at the local GMS `Data` directory, Core tests validate representative
  package roots, `Map1.wz` package-group image identity, and
  `Map1_000.wz/100000000.img` `miniMap/_outlink` resolved target identity.
- A local GMS M5 inventory found 780 `.wz` files, all PKG1 by `headers` scan;
  no local `List.wz`, `.mn`, or PKG2 WZ sample was found, but 10
  `Data/Packs/*.ms` files exist. The next sample-driven container step should
  review WC `Ms_File` / `Ms_FileV2` before implementing a minimal `.ms`
  inspection slice.
- Lua IMG and WC text-format IMG behavior is locked by synthetic fixtures, but
  still needs direct real-sample smoke verification when a suitable local
  client entry is found.

## Suggested Next Prompt

Use this in a new Codex project conversation:

```text
We are continuing the WCX modernization project in this repository. Please read
AGENTS.md, docs/README.md, docs/handoff.md, docs/roadmap.md, and
docs/development-guidelines.md first. Continue post-M4 cleanup and Milestone 5
Parser Coverage And Resource Model Baseline planning. Reuse Core
inspection/export models, keep parsing out of view models, use
docs/roadmap.md's WC/WCX capability matrix plus
docs/parser-coverage-matrix.md to choose parser/resource-model slices, add
focused tests where practical, run build/test, update docs/logs, and commit the
work on the current branch.
```

## Caution

Do not push to any remote unless the user explicitly asks. In the current local
clone, `origin` points to `Xseventh/WzComparerX` and `upstream` points to
`Kagamia/WzComparerX`.
