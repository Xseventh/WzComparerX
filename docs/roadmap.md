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

Status: in progress.

Goal: UI can browse the same workspace model as CLI.

Tasks:

- Open file/folder command. Started with one `Browse` entry containing
  WC-aligned `Open Wz...` and folder picker choices. `Load` automatically
  handles file paths or folder paths. Folder inspection lists discovered WZ
  packages, and selected packages can be opened from the tree. After IMG
  inspection, `Open Package` returns to the current package directory.
- Node tree view. Started with a path-based load command bound to Core
  inspection; selected image nodes can be loaded through the same inspect path,
  and double-click activation opens package/image nodes through ViewModel
  commands. Redundant activation of the already-open package or already-selected
  IMG is suppressed at the command-state layer. Empty split-package stubs now
  expand into depth-bounded linked package nodes when the package workspace
  layout is present.
- IMG selector workflow. Started with manual selector inspection through the
  same `Inspect Image` action used for selected image nodes.
- Property panel. Started with selected-node metadata and diagnostics.
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

## Milestone 5: Compare Foundation

Goal: restore WC's central comparison value in a cleaner shape.

Tasks:

- Define compare model.
- Add CLI compare command.
- Add JSON report.
- Add filters.
- Later: HTML or UI report.

Exit criteria:

- Two fixture trees can be compared deterministically.

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
