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

## Milestone 3: Export And Inspect

Goal: make CLI useful for automation.

Tasks:

- Add `inspect`.
- Add JSON dump.
- Add XML dump if parser coverage allows.
- Add export abstraction in Core.
- Add error diagnostics model.

Exit criteria:

- CLI can inspect and dump at least synthetic/raw nodes.
- Output is covered by snapshot-like expected files.

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

- Image preview/export.
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
