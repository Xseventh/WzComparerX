# Development Guidelines

These rules are meant to keep WCX understandable across long development gaps.

## Engineering Principles

- Preserve behavior with tests before reshaping migrated WC code.
- Keep UI out of parsing and domain logic.
- Prefer explicit models over raw node dictionaries at module boundaries.
- Make CLI workflows available before UI-only workflows.
- Keep changes small enough to review and revert.
- Record meaningful architectural choices in `docs/adr/`.
- WCX is still in early development. Do not preserve awkward internal APIs just
  for compatibility with previous WCX commits; prefer timely refactors when a
  cleaner module boundary appears.
- New parser behavior should be reachable through `inspect` before it is treated
  as complete. Do not add new parser behavior behind separate diagnostic CLI
  surfaces; use `inspect --debug` for low-level development metadata.

## Project Boundaries

### WzComparerX.WzLib

Allowed:

- Binary parsing.
- Format-version detection.
- Encryption and decryption helpers.
- Lazy resource extraction.
- Minimal raw resource metadata.

Not allowed:

- Avalonia.
- WinForms/WPF/WinUI.
- MonoGame or DirectX.
- App settings or plugin host concepts.

### WzComparerX.Core

Allowed:

- Workspace/session orchestration.
- File open/close workflows.
- Stable inspect models used by CLI, UI, export, and search workflows.
- Structured inspect diagnostics and debug metadata. Diagnostics should include
  stable codes for user-visible or automatable conditions.
- Search, export, compare services.
- Progress, cancellation, diagnostics abstractions.

Not allowed:

- UI controls.
- Direct rendering backend references.
- Global mutable singletons for current workspace state.

### WzComparerX.Domain

Allowed:

- Explicit MapleStory semantic models.
- Node-to-domain projection.
- String linking and metadata normalization.

Not allowed:

- Pixel rendering.
- UI layout.
- File-system scanning unrelated to domain loading.

### WzComparerX.Rendering

Allowed:

- Renderer-independent scene/frame/bitmap projection.
- Platform-specific renderer adapters behind interfaces.

Not allowed:

- Loading arbitrary WZ files by itself.
- Owning workspace state.

### WzComparerX.App

Allowed:

- Avalonia UI.
- MVVM view models.
- User settings and window state.
- Command binding to Core services.

Not allowed:

- Reimplementing parsing logic.
- Directly depending on WC code.

## Coding Style

- Use C# with nullable reference types enabled.
- Prefer immutable records for data transfer models.
- Prefer services with small interfaces at module boundaries.
- Use `CancellationToken` for potentially slow IO or parsing operations.
- Avoid static mutable state unless it is clearly safe and documented.
- Avoid catching broad exceptions unless the layer converts them into diagnostics.

## Tests

Every migrated parser behavior should have a test or a documented reason it
cannot yet be tested.

Test names should describe behavior:

```csharp
public void OpenSyntheticDirectoryTree_ReturnsExpectedRoot()
```

Fixture tests should prefer small files in `fixtures/synthetic`. If a real
client sample is needed, keep it out of git and document it under
`fixtures/external`.

M4 Avalonia UI behavior belongs in `WzComparerX.App.Tests`, not
`WzComparerX.Core.Tests`. Use Avalonia Headless tests for window/control
structure, binding smoke checks, and future screenshot-assisted layout
verification. Keep Core tests focused on Core services, CLI behavior, and
parser-facing models.

## Git Workflow

- Work on feature branches, not `master`.
- Keep commits focused and named by outcome.
- Run build and tests before committing unless explicitly blocked.
- Do not commit `bin/`, `obj/`, full client files, or local-only fixture data.
- Use commit messages in imperative or concise descriptive style.

Good examples:

- `Add initial WCX architecture docs`
- `Introduce raw WZ node model`
- `Add CLI list command`

## Documentation Rules

Update docs when:

- A boundary changes.
- A dependency is added.
- A migrated WC behavior is accepted or rejected.
- A known limitation is discovered.
- A new fixture policy or format observation appears.

For significant decisions, add an ADR:

```text
docs/adr/0002-use-avalonia-for-desktop-ui.md
```

For day-to-day progress, append a short log file under `docs/logs/`.

## Dependency Rules

- Prefer .NET SDK and BCL features before adding packages.
- Add packages only when they remove meaningful complexity.
- Keep package usage local to the layer that needs it.
- Avoid Windows-only dependencies outside platform-specific app/rendering code.
- Document new dependencies in the commit message or ADR when they shape the
  architecture.

## Migration Rules

When migrating from WC:

1. Link the source files in `docs/migration-from-wc.md` or a feature note.
2. Port the minimum behavior needed.
3. Keep names close at first if it helps verification.
4. Add tests.
5. Refactor after tests pass.

Do not port `MainForm` patterns, global `PluginManager.FindWz`, or Designer UI
code into WCX core layers.

## Inspect Rules

- `inspect` is the preferred stable resource observation surface.
- `inspect --debug` is the preferred long-term diagnostic surface.
- Parser selection, key detection, property-depth behavior, and user-facing
  resource output should flow through `inspect`.
- If a low-level field is useful beyond one parser migration session, add it to
  structured inspect metadata or diagnostics instead of formatting it only in a
  separate command.
- Preserve underlying parser coverage and tests through `inspect` /
  `inspect --debug`.
