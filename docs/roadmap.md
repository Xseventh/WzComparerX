# Roadmap

This roadmap is intentionally practical. WCX should earn each layer before
trying to match WC's full feature set.

## Milestone 0: Project Bootstrap

Status: done.

- .NET solution skeleton.
- Avalonia app shell.
- CLI project.
- Core, domain, rendering, WzLib projects.
- xUnit test projects.
- Architecture and migration docs.

## Milestone 1: Headless Resource Browser

Status: done.

Goal: prove the core can model and browse a resource tree without UI.

Tasks:

- Define raw node model in `WzComparerX.WzLib`.
- Define document/workspace model in `WzComparerX.Core`.
- Add synthetic fixture format or minimal hand-built fixture.
- Implement `wcx list`.
- Add tests for node enumeration.
- Add tests for deterministic CLI output.

Exit criteria:

- `dotnet test` covers the synthetic fixture.
- `dotnet run --project src/WzComparerX.Cli -- list <fixture>` prints a stable
  tree.

## Milestone 2: First Real WC Migration

Status: done.

Goal: migrate the smallest useful WC parsing behavior.

Candidate scope:

- Header/version detection, or
- Directory tree enumeration, or
- Scalar IMG value parsing.

Tasks:

- Identify WC source files.
- Port minimal code into `WzComparerX.WzLib`.
- Remove Windows/UI assumptions.
- Add fixture-backed tests.
- Document unsupported cases.

Exit criteria:

- One real parser behavior is represented in WCX.
- Tests lock behavior.
- `docs/format-notes.md` is updated.

Closeout notes:

- Real PKG1 directory and IMG inspection behavior exceeded the original scope
  and is now routed through `inspect` / `inspect --debug`.
- M2 covered header detection, PKG1 directory traversal, string-key handling,
  IMG object/property metadata, Lua/text IMG inspection, and payload metadata
  for Canvas, RawData, Video, and Sound values.
- Unsupported decode/export work has moved to M3 or later backlog items.

## Milestone 3: Export And Inspect

Status: done.

Goal: make CLI useful for automation.

Tasks:

- Add `inspect`.
- Add `inspect --debug` and structured parser diagnostics.
- Add WZ package header CLI output.
- Add JSON dump. Implemented for `list`, `header`, `headers`, `inspect`, and
  metadata export.
- Add XML dump if parser coverage allows. Deferred; JSON and metadata export
  cover the M3 automation requirement.
- Add export abstraction in Core. Implemented for metadata, text IMG, Lua, and
  direct-zlib Canvas raw bytes, with `--out` file output for exact bytes.
- Add error diagnostics model. Implemented initial shared diagnostics factory,
  stable severity/source/code constants, shared CLI text formatting, and
  `docs/diagnostics.md`.
- Define first Canvas decode/export slice. Documented in
  `docs/canvas-decode-export-plan.md`; WzLib direct-zlib decoder and raw-byte
  CLI export are covered by committed synthetic fixtures.
- Continue export surface decisions for stdout vs `--out`, text vs binary
  content, and partial/unsupported payload diagnostics.
- Define and implement explicit Canvas value selection for export. Implemented
  with `--value <property-path>` and documented in
  `docs/canvas-export-selector-plan.md`.

Exit criteria:

- CLI can inspect and export synthetic/raw nodes plus metadata, text IMG, Lua
  IMG, and the first raw Canvas byte slice.
- Output is covered by snapshot-like expected files.
- Parser diagnostics are available through `inspect --debug`.
- Diagnostics rules are documented and tested.
- Canvas export does not rely on "first Canvas wins"; callers select a Canvas
  value explicitly and that behavior has CLI golden coverage.
- PNG export is not required for M3. It remains a later user-facing image export
  milestone after raw Canvas bytes, value selection, and diagnostics are stable.

Closeout notes:

- M3 establishes `inspect`, `inspect --debug`, and `export` as the headless
  automation surfaces.
- Export currently covers metadata JSON, WC text-format IMG, Lua IMG, and the
  first raw Canvas byte slice.
- Diagnostics have stable severities, sources, codes, CLI text formatting, and
  docs.
- M4 should now build UI browsing on top of Core inspection/export models
  instead of adding parser behavior directly to the app layer.

## Milestone 4: Basic Avalonia Browser

Status: done.

Goal: UI can browse the same workspace model as CLI.

Tasks:

- Open file/folder command. Started with one `Browse` entry containing
  WC-aligned `Open Wz...` and folder picker choices. `Load` automatically
  handles file paths or folder paths. Folder inspection lists discovered WZ
  packages, and selected packages can be opened from the tree.
- Node tree view. Started with a path-based load command bound to Core
  inspection; selected image nodes are extracted into a separate IMG Content
  tree through the same inspect path,
  and double-click activation opens package nodes or refreshes image content
  through ViewModel commands. Empty split-package stubs now expand eagerly into
  linked WC-style package group nodes when the package workspace layout is
  present, and entry packages merge numbered shards from `Name.ini` /
  `Name_000.wz...` while preserving each image node's original source package.
- IMG selector workflow. The selector row now uses `Load IMG` only for manual
  selectors or refreshes; selected image nodes auto-load IMG Content. UI
  inspection follows WC's lazy/full IMG model: only the selected IMG is loaded,
  and it is inspected as a complete IMG Content tree without replacing the
  resource tree.
- Property panel. Started with selected-node metadata and diagnostics.
- Canvas preview. Started with lazy Canvas value preview through Core using the
  current direct-zlib format `1` / `2` decoder slice. Preview now follows IMG
  Content selection: Canvas nodes preview exact values, root Canvas IMG objects
  share the same viewer path, and `source` / `_inlink` / `_outlink` strings can
  resolve linked Canvas values. Small bitmaps are displayed with capped integer
  scaling, very large bitmaps shrink in `Auto`, and explicit `1x`, `2x`, `4x`,
  `8x`, and `16x` controls remain available.
- Log/task panel. Started with a deterministic activity log for UI load,
  inspection, and error events.
- UI test harness. Started with `WzComparerX.App.Tests` using Avalonia Headless
  plus migrated ViewModel tests; Skia-backed headless screenshot smoke coverage
  now checks rendered content, primary-control viewport bounds, and visible
  invalid path/folder/package/image inspection states.
- No direct parsing in view models.

Exit criteria:

- UI uses Core services.
- UI can browse fixture/sample data.
- Resources tree, IMG Content tree, and Canvas Preview are covered by
  ViewModel/headless tests.
- Local GMS smoke records package group merge, selected IMG extraction, Canvas
  Preview, large directory behavior, and large Canvas auto-scaling.
- Direct-zlib Canvas preview for formats `1` / `2` is enough for M4 closeout.
  Broader Canvas decode, PNG export, MapRender, search, and compare remain
  later milestones.
- Larger `MainWindowViewModel` workflow splitting and Core package-group helper
  cleanup are planned immediately after M4 closeout, not during final UI smoke
  stabilization.

Closeout notes:

- M4 establishes a usable Avalonia browser shell over the Core inspection model.
- Resources tree, IMG Content tree, Canvas Preview, linked Canvas preview, and
  WC-style package group merge are covered by App tests and local GMS smoke.
- Direct-zlib Canvas preview formats `1` / `2` are accepted as the M4 image
  preview slice; broader decode and PNG export stay later.
- Post-M4 cleanup has started by extracting Canvas Preview workflow logic out of
  `MainWindowViewModel`.

## Milestone 5: Compare Foundation

Status: not started.

Goal: restore WC's central comparison value in a cleaner shape.

Tasks:

- Define compare model.
- Add CLI compare command.
- Add JSON report.
- Add filters.
- Later: HTML or UI report.

Exit criteria:

- Two fixture trees can be compared deterministically.

## WC Feature Compatibility Backlog

WCX is a successor to WC, but it should not copy every WC feature as-is. The
feature backlog below separates the capabilities that should be preserved from
features that need redesign or may be replaced by modern project workflows.

### Features WCX Should Eventually Support

- Core resource browsing:
  - open WZ packages, standalone IMG files, and folders;
  - keep WC-style lazy image extraction;
  - support node details, metadata, diagnostics, history, copy path, and
    useful context actions.
- Parser coverage:
  - complete PKG1 coverage;
  - full PKG2 directory parsing and image offset/version profiles;
  - List.wz handling if it remains needed for target client samples;
  - `.ms` / `.mn` container support if current client data still depends on
    them;
  - split-package and Base.wz linking behavior.
- IMG/resource values:
  - full scalar/property traversal;
  - UOL, link, `_inlink`, `_outlink`, and `source` resolution;
  - Canvas, Sound, RawData, Video, Lua, and text-format IMG coverage.
- Image and animation workflows:
  - broader Canvas pixel format decode matrix;
  - PNG export;
  - raw payload export;
  - animation frame extraction;
  - GIF/APNG/video export after the rendering/export model is stable.
- Sound and video workflows:
  - Sound_DX8 payload extraction;
  - mp3/wav/pcm export;
  - playback can come later than deterministic export;
  - Canvas#Video / VPX video decode should be evaluated after image export.
- Search and linking:
  - node search by name, value, image node, and full path;
  - StringLinker indexing for equipment, items, maps, mobs, NPCs, and skills;
  - search-result navigation back into the resource browser.
- Compare:
  - deterministic Core compare model;
  - CLI compare and JSON report first;
  - filters and PNG/link-aware comparisons;
  - HTML/UI report later.
- Domain projections:
  - item, gear, skill, recipe, mob, NPC, familiar, damage-skin, set-item, and
    similar typed models;
  - tooltip data projection before pixel-perfect tooltip rendering.
- User-facing export:
  - metadata, JSON, WC text IMG, Lua, Canvas, PNG, sound, and compare reports;
  - database/CSV-style export only if there is a concrete downstream workflow.
- App settings:
  - string key / encoding choices;
  - split-package detection options;
  - export and preview defaults;
  - persisted recent paths and user preferences.

### Large-Refactor Feature Families

These WC modules are important, but should not be ported directly. When the
project reaches them, write a dedicated design plan before implementation.

- CharaSim:
  - requires a clean domain model split from tooltip rendering and WinForms
    controls;
  - should start with typed data projection and headless tests before UI.
- Avatar:
  - requires a modern composition/rendering model for body parts, actions,
    emotions, taming, frame layers, and export;
  - should share rendering/export primitives with other animation workflows.
- MapRender:
  - requires a renderer-independent map scene model before any Avalonia or
    game-loop UI;
  - map data, resources, particles, lights, minimap, portals, footholds, life
    objects, and BGM should be migrated in slices.

### Features To Reconsider Or Replace

- Network chat:
  - likely not core to WCX and should be skipped unless a real user workflow
    appears.
- Auto updater:
  - prefer GitHub releases, package managers, or platform update tooling before
    rebuilding WC's custom updater.
- Plugin SDK:
  - useful long-term, but should wait until Core/App boundaries are stable.
- Patcher:
  - keep as a possible separate tool; do not let it block browser, compare,
    parser, or rendering milestones.
- Lua console:
  - keep as optional and later; scripts should use stable Core APIs instead of
    global UI state.

## Later Milestones

- Image rendering/export.
- String search and StringLinker.
- CharaSim domain model.
- Tooltip data projection.
- Avatar model and export.
- Map data model.
- Rendering backends.
- Plugin SDK.

## Backlog Notes

- Keep WCX useful before it is complete.
- Avoid making MapRender block core browser progress.
- Prefer CLI and tests for every feature before UI polish.
- Full PKG2 directory parsing.
- PNG export and a broader Canvas pixel decode matrix.
- Audio and video payload decoding.
- UI browsing beyond the current Avalonia shell.
- XML dump, if a concrete downstream workflow needs it.
