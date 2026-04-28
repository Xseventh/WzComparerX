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

Goal: make CLI useful for automation.

Tasks:

- Add `inspect`.
- Add `inspect --debug` and structured parser diagnostics.
- Add WZ package header CLI output.
- Add JSON dump. Implemented for `list`, `header`, `headers`, `inspect`, and
  metadata export.
- Add XML dump if parser coverage allows.
- Add export abstraction in Core. Started for metadata, text IMG, Lua, and
  direct-zlib Canvas raw bytes, with `--out` file output for exact bytes.
- Add error diagnostics model. Implemented initial shared diagnostics factory,
  stable severity/source/code constants, shared CLI text formatting, and
  `docs/diagnostics.md`.
- Define first Canvas decode/export slice. Documented in
  `docs/canvas-decode-export-plan.md`; WzLib direct-zlib decoder and raw-byte
  CLI export are started and covered by committed synthetic fixtures.
- Continue export surface decisions for stdout vs `--out`, text vs binary
  content, and partial/unsupported payload diagnostics.
- Define and implement explicit Canvas value selection for export. The design
  is tracked in `docs/canvas-export-selector-plan.md`.

Exit criteria:

- CLI can inspect and export synthetic/raw nodes plus metadata, text IMG, Lua
  IMG, and the first raw Canvas byte slice.
- Output is covered by snapshot-like expected files.
- Parser diagnostics are available through `inspect --debug`.
- Diagnostics rules are documented and tested.
- Canvas export does not rely on "first Canvas wins"; callers can select a
  Canvas value explicitly and that behavior has CLI golden coverage.
- PNG export is not required for M3. It remains a later user-facing image export
  milestone after raw Canvas bytes, value selection, and diagnostics are stable.

## Milestone 4: Basic Avalonia Browser

Goal: UI can browse the same workspace model as CLI.

Tasks:

- Open file/folder command.
- Node tree view.
- Property panel.
- Log/task panel.
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
