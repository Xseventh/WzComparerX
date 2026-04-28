# 2026-04-28 - Milestone 3 Closeout

## Completion Definition

Milestone 3 is complete. WCX now has stable headless automation surfaces for
inspection, development diagnostics, and initial export workflows.

## Completed Scope

- `inspect` and `inspect --debug` are the shared resource observation surfaces.
- CLI JSON output is covered for list, header, header scan, inspect, and
  metadata export workflows.
- Core export abstraction supports:
  - metadata JSON,
  - WC text-format IMG streams,
  - Lua IMG scripts,
  - direct-zlib raw Canvas bytes for the first supported Canvas format slice.
- Text exports can write to stdout or exact bytes with `--out`.
- Binary export requires `--out`.
- Canvas export uses `--value <property-path>` for IMG-internal Canvas values.
- Diagnostics have stable severity, source, code, path, CLI text formatting, and
  documentation.
- CLI-visible export and diagnostics behavior is covered by deterministic tests
  and expected-output fixtures.

## Deferred Work

- PNG export.
- Broader Canvas pixel decode matrix.
- Audio and video payload decoding.
- RawData payload decoding.
- Full PKG2 directory parsing.
- XML dump, unless a concrete workflow needs it.
- Direct real-client smoke verification for Lua IMG and WC text-format IMG
  samples when suitable local entries are found.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`

## Next

Start Milestone 4: Basic Avalonia Browser. The UI should bind to Core
inspection/export services and avoid parser logic in view models.
