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
  WC-aligned `Open Package...` and folder picker choices. `Load` automatically
  handles file paths or folder paths. Folder inspection lists discovered WZ,
  MS, and MN packages, and selected packages can be opened from the tree.
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
- Canvas preview. Started with lazy Canvas value preview through Core using
  direct-zlib `ARGB4444` (`1`), `ARGB1555` (`257`), `RGB565` (`513`), `R16`
  (`769`), `ARGB8888` (`2`), `A8` (`2304`), `RGBA1010102` (`2562`), `DXT3`
  (`1026`), `DXT5` (`2050`), `DXT1` (`4097`), `BC7` (`4098`),
  `RGBA32Float` (`4100`), and WC's `RGB565 scale=4` expansion decoder slices.
  Preview now follows IMG Content selection: Canvas nodes preview exact values, root Canvas IMG objects
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
- Direct-zlib Canvas preview for formats `1` / `2` was enough for M4 closeout.
  M5 has since added `257`, `513`, `769`, `2304`, `2562`, `1026`, `2050`,
  `4097`, `4098`, and `4100`; PNG export,
  MapRender, search, and compare remain later milestones.
- Larger `MainWindowViewModel` workflow splitting and Core package-group helper
  cleanup are planned immediately after M4 closeout, not during final UI smoke
  stabilization.

Closeout notes:

- M4 establishes a usable Avalonia browser shell over the Core inspection model.
- Resources tree, IMG Content tree, Canvas Preview, linked Canvas preview, and
  WC-style package group merge are covered by App tests and local GMS smoke.
- Direct-zlib Canvas preview formats `1` / `2` were accepted as the M4 image
  preview slice; M5 extends this to `257`, `513`, `769`, `2304`, `2562`, `1026`,
  `2050`, `4097`, `4098`, and `4100`,
  while PNG export stays later.
- Post-M4 cleanup has started by extracting Canvas Preview and IMG Content
  workflow logic plus resource detail projection out of `MainWindowViewModel`;
  Core split-package link path resolution has also been split from the main
  inspection service.

## Milestone 5: Parser Coverage And Resource Model Baseline

Status: active.

Goal: stabilize the parsing and resource-identity foundation before compare,
search, export, and richer UI workflows start depending on it.

This milestone intentionally comes before Compare Foundation. M4 proved the
Avalonia browser can consume Core inspection services, but WCX still needs
broader real-client parser coverage and a more explicit resource identity
contract before higher-level WC-compatible features are safe to build.

Must support:

- Parser coverage matrix:
  - document WC support, WCX support, gap, priority, and validation method for
    package formats, IMG values, media payloads, search, compare, domain
    projections, and app features;
  - keep the matrix current as new parser slices land;
  - use `docs/parser-coverage-matrix.md` as the working M5 parser/resource
    identity checklist.
- PKG1 completion pass:
  - recursive directory edge cases;
  - string-reference names;
  - hash version, hash offset, checksum, and image offset boundaries;
  - large directory behavior and stable diagnostics.
- PKG2 foundation:
  - directory parsing beyond header detection;
  - image offset and version/hash profile handling;
  - representative real-client smoke notes.
- Legacy / optional container coverage:
  - support remains required for `List.wz`, `.ms`, and `.mn` compatibility,
    especially for older clients;
  - current local GMS data has `Data/Packs/*.ms` samples but no `List.wz` or
    `.mn` sample, so `.ms` should proceed sample-first while `.mn` keeps
    synthetic coverage through the same WC MS loader path and `List.wz` should
    be defined from WC reference behavior or older-client samples;
  - unsupported container paths should return stable diagnostics until parser
    coverage lands.
- Split-package and Base.wz linking:
  - `Name.wz` plus `Name_000.wz...` package groups;
  - `.ini` `LastWzIndex` handling;
  - `Base/Base.wz` links into child packages;
  - linked image nodes preserve their true source package and value path;
  - failed link resolution emits stable diagnostics.
- IMG/resource values:
  - scalar values and nested `Property` traversal;
  - `Vector`, `Convex2D`, `UOL`, `Canvas`, `Sound_DX8`, `RawData`,
    `Canvas#Video`, Lua, and text-format IMG coverage;
  - useful metadata for payload offsets, lengths, compression, and texture
    formats.
- Link semantics:
  - UOL, `link`, `_inlink`, `_outlink`, and `source` resolution;
  - resolved target paths represented in Core inspection metadata;
  - unresolved links represented as diagnostics, not silent omissions.
- Resource model contract:
  - stable node identity, full path, package source, image selector, value
    path, linked target, metadata, and diagnostics code semantics;
  - Core inspection model remains the shared surface for CLI, UI, export,
    search, and compare.
- Validation:
  - extend `inspect --debug` where low-level fields are useful beyond one
    migration session;
  - add CLI batch/smoke workflows if needed for local client scans;
  - record real-client smoke notes for Map, UI, Item, Character, String, Skill,
    Mob/Npc, Sound, and Effect-style packages without committing client files.

Deferred:

- Compare reports and UI compare views.
- Full StringLinker/search UX.
- Complete Canvas pixel matrix and PNG export unless a small parser slice needs
  them to validate resource identity.
- Sound playback and video playback.
- CharaSim, Avatar, and MapRender.

Exit criteria:

- The roadmap contains an explicit WC/WCX capability matrix that developers can
  use to choose parser and feature slices.
- `docs/parser-coverage-matrix.md` stays current with accepted M5 parser and
  resource-model slices.
- PKG2 has progressed beyond header-only support or has a documented blocker.
- Legacy container support has at least a documented first-slice plan for
  `List.wz`, `.ms`, and `.mn`, with stable diagnostics for unsupported paths.
- The Core inspection model can represent package source, image selector, value
  path, and linked target semantics for the next compare/search milestones.
- At least several representative real-client smoke notes cover different
  package families.
- Build and tests pass after the accepted parser/model slices.

## Milestone 6: Compare Foundation

Status: planned.

Goal: restore WC's central comparison value in a cleaner, testable shape after
the resource model baseline is stable.

Must support:

- Core compare model.
- CLI `compare` command.
- JSON report.
- Compact text summary.
- Add, remove, and change classifications.
- Node kind, metadata, and scalar value differences.
- Path filters and kind filters.
- `only-added`, `only-removed`, and `only-changed` style report filters.
- Deterministic fixture-based tests.

Deferred:

- HTML report.
- Avalonia compare viewer.
- Image-aware pixel comparisons.
- Link-aware comparison beyond what M5 already exposes in Core metadata.
- Custom report colors and CSS.

Exit criteria:

- Two fixture or sample inspection trees can be compared deterministically.
- CLI text and JSON outputs have golden coverage.
- The compare implementation depends on Core inspection/resource contracts, not
  App view models or WzLib-only DTOs.

## Milestone 7: Search And StringLinker Foundation

Status: planned.

Goal: rebuild WC's resource search and StringLinker value in a headless-first
form.

Must support:

- SearchWzNode-style search:
  - node name search;
  - image node search;
  - image value search;
  - full path search;
  - kind/type filters;
  - stable result paths for UI navigation.
- StringLinker-style indexes:
  - Eqp;
  - Item;
  - Map;
  - Mob;
  - NPC;
  - Skill;
  - additional categories such as Quest, Recipe, SetItem, Familiar, or Damage
    Skin only after sample-driven validation.
- CLI search surface.
- Core search index model.
- Missing or unresolved string diagnostics.

Deferred:

- Advanced fuzzy search.
- Full UI search panel polish.
- Domain-specific tooltips.

Exit criteria:

- Search can run without the Avalonia app.
- Search results can navigate back to resource paths.
- StringLinker indexes have fixture or real-sample smoke coverage.

## Milestone 8: Image And Media Decode / Export Expansion

Status: planned.

Goal: move beyond the narrow M4 Canvas preview slice toward practical image,
animation, sound, and video export workflows.

Must support:

- Broader Canvas pixel format decode matrix.
- PNG export.
- Raw Canvas payload export.
- Batch image export.
- Animation frame extraction.
- Sound_DX8 payload extraction.
- MP3/WAV/PCM export where the source payload supports it.
- Stable unsupported-format diagnostics.

Deferred:

- GIF/APNG/video export until the frame model is stable.
- Sound playback until deterministic export is reliable.
- Canvas#Video / VPX preview until image export is mature enough to justify the
  extra decoder surface.
- FFmpeg configuration UI.

Exit criteria:

- Common real-client Canvas images can be exported as PNG.
- At least one sound export path works through CLI and tests/smoke notes.
- Unsupported payloads fail with stable diagnostics instead of raw exceptions.

## Milestone 9: App Browser Maturity

Status: planned.

Goal: turn the M4 basic Avalonia browser into a practical daily resource
browser while keeping parser/domain behavior in Core.

Must support:

- Recent files and recent folders.
- App settings for string key, split-package detection, preview scale, export
  defaults, and user preferences.
- Tree search and filtering.
- Context menus for copy path, copy value, copy diagnostics, inspect, export,
  and open linked target.
- History navigation.
- Progress and cancellation for slow loads.
- Large WZ responsiveness.
- Diagnostics panel polish.
- Export UI for supported Core exporters.
- Separate image, sound, text, and metadata preview workflows as those Core
  capabilities land.

Deferred:

- Compare UI until M6 is stable.
- Search UI polish until M7 is stable.
- Heavy renderer/game-loop features.

Exit criteria:

- The UI remains a consumer of Core services.
- Common browser tasks are available without using the CLI.
- Avalonia Headless and ViewModel tests cover the primary workflows.

## Milestone 10: Domain Projection Foundation

Status: planned.

Goal: prepare the data layer needed for QuickView, tooltip, CharaSim, Avatar,
and MapRender without porting old WinForms-era coupling.

Must support:

- Item model.
- Equip model.
- Skill model.
- Mob model.
- NPC model.
- Map model.
- Recipe model.
- Set item model.
- Familiar model.
- Damage skin model.
- Tooltip data projection.
- Domain model tests.
- StringLinker integration where names and categories are required.

Deferred:

- Pixel-perfect tooltip rendering.
- CharaSim UI.
- Avatar rendering/export.
- MapRender scene playback.

Exit criteria:

- Domain projections are testable without UI.
- Tooltip and simulator features can consume explicit typed models instead of
  raw resource nodes.

## Milestone 11+: Large Feature Families

Status: future design required.

These WC modules are important, but should not be ported directly. Each family
needs a dedicated design note before implementation starts.

- QuickView / Tooltip:
  - data projection first;
  - renderer second;
  - UI integration last.
- CharaSim:
  - inventory;
  - equipment slots;
  - character stats;
  - set effects;
  - simulator workflows;
  - tooltip integration.
- Avatar:
  - body part composition;
  - action and emotion selection;
  - taming/mount support;
  - layer ordering;
  - code import;
  - PNG/GIF export;
  - shared rendering/export primitives.
- MapRender:
  - renderer-independent map scene model first;
  - layers;
  - objects;
  - footholds;
  - portals;
  - life objects;
  - particles;
  - lights;
  - minimap;
  - BGM;
  - renderer adapter after the scene model is stable.
- Patcher:
  - keep as a separate later tool;
  - do not let patching block parser, browser, compare, search, or media
    milestones.
- Plugin SDK:
  - useful long-term;
  - wait until Core/App/Rendering boundaries are stable.

## WC/WCX Capability Matrix

This matrix is intentionally high level. It should be refined as M5 adds real
parser coverage and smoke notes.

| Capability | WC support | WCX current state | Approximate coverage | Roadmap owner |
| --- | --- | --- | --- | --- |
| Basic resource browsing | WZ/IMG/MS/MN open, three-tree browsing, details, context menus, history | WZ/MS/MN/file/folder/fixture open, Resources + IMG Content + Preview | ~45% | M9 |
| Package parsing | PKG1, PKG2, Base/extension packages, `.ms`/`.mn`, List.wz, newer KMST formats | PKG1 mainline; PKG2 KMST1199/1200 directory and image-offset first slice; `.ms`/`.mn` directory tables and initial image payload extraction; List.wz first-slice inspection | ~50-55% | M5 |
| IMG property parsing | Property, Vector, Convex, UOL, Canvas, Sound, RawData, Video, Lua, text IMG | Most have inspection metadata; payload behavior is shallow | ~50% | M5 |
| Canvas/image decode | Multiple pixel formats, display, PNG save, raw export | Direct-zlib viewer slices for `1`/`257`/`513`/`513 scale=4`/`769`/`2`/`2304`/`2562`/`1026`/`2050`/`4097`/`4098`/`4100`; raw export remains the initial direct-zlib Canvas byte slice | ~45% | M8 |
| Animation/GIF/APNG/video export | Frame extraction, GIF/APNG, FFmpeg settings | No complete animation export | ~0-5% | M8 |
| Sound playback/export | Sound_DX8 extract, play, pause, loop, save mp3/wav/pcm | Sound metadata only | ~5% | M8 |
| Canvas#Video/video | VPX/video load and preview paths | Video metadata only | ~0-5% | M8 |
| SearchWzNode | Search by node name, image node, image value, full path | No formal search feature | 0% | M7 |
| StringLinker/SearchString | Eqp/Item/Map/Mob/Npc/Skill string indexes, search, jump to resource | No StringLinker | 0% | M7 |
| Compare | EasyCompare, HTML report, CSS/colors, PNG output, link resolution | Not started | 0% | M6 |
| Database/CSV export | Skill and SkillOption CSV export | None | 0% | M10 or later |
| QuickView/Tooltip | Gear, Item, Skill, Recipe, Mob, NPC, Familiar, DamageSkin tooltips | No Domain/Tooltip UI | 0% | M10/M11 |
| CharaSim | Inventory, equipment, stats, set effects, character simulator UI | None | 0% | M11+ |
| Avatar/paper doll | Avatar plugin, parts, actions, emotions, mounts, code import, GIF/PNG save | None | 0% | M11+ |
| MapRender | Map simulator, layers, objects, footholds, portals, life, particles, lights, minimap, BGM | None | 0% | M11+ |
| Patcher | Manual patch, reverse patcher, patch checks, added/deleted/PNG output | None | 0% | M11+ separate tool |
| LuaConsole | Script open/save/run/stop, WZ-bound scripting environment | None | 0% | Optional later |
| Network | Chat/log/server connection plugin | None | 0%; likely not worth migrating | Reconsider |
| Auto updater | Custom updater project and restart flow | None | 0%; prefer modern release tooling | Replace |
| Plugin system | PluginBase, plugin loading, Ribbon/Tab injection, WzOpened/SelectedNode events | None | 0% | Late M11+ |
| Settings/config | Encoding, extension packages, IMG checksum skip, QuickView/GIF/Compare/MapRender settings | Minimal UI inputs | ~5-10% | M9 |
| Tests/automation | Traditional desktop code with weak tests | CLI/Core/UI tests are stronger than WC | WCX advantage | Continuous |

## Features To Reconsider Or Replace

- Network chat:
  - likely not core to WCX and should be skipped unless a real user workflow
    appears.
- Auto updater:
  - prefer GitHub releases, package managers, or platform update tooling before
    rebuilding WC's custom updater.
- Plugin SDK:
  - useful long-term, but should wait until Core/App/Rendering boundaries are
    stable.
- Patcher:
  - keep as a possible separate tool; do not let it block browser, compare,
    parser, or rendering milestones.
- Lua console:
  - keep as optional and later; scripts should use stable Core APIs instead of
    global UI state.

## Backlog Notes

- Keep WCX useful before it is complete.
- Prefer CLI and tests for every feature before UI polish.
- Treat WC as a format and workflow reference, not a UI architecture target.
- Avoid making MapRender, Avatar, or CharaSim block parser/browser progress.
- Revisit the capability matrix during weekly progress reviews.
- XML dump remains optional unless a concrete downstream workflow needs it.
