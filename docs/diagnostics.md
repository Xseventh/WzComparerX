# Diagnostics

Diagnostics are the stable problem/status model shared by inspect, export, CLI
formatting, and future UI surfaces.

They are different from debug metadata:

- Debug metadata describes low-level parser facts such as offsets, sizes,
  selected keys, or decoded object types.
- Diagnostics describe a condition that a caller may need to display, filter,
  automate, or treat as partial success/failure.

## Model

Diagnostics use `ResourceInspectionDiagnostic`:

- `Severity`: stable lowercase severity string.
- `Message`: human-readable English message.
- `Path`: optional resource selector or inspection path.
- `Code`: optional stable machine-readable code.
- `Source`: optional subsystem that produced the diagnostic.

Use the smallest stable resource path that identifies the affected value. For
exports, a Canvas payload diagnostic should point at `icon` or `body/icon` when
that property path is known, not only at the parent IMG selector.

Use the named constants in Core:

- `ResourceDiagnosticSeverities.Info`
- `ResourceDiagnosticSeverities.Warning`
- `ResourceDiagnosticSeverities.Error`
- `ResourceDiagnosticSources.Parser`
- `ResourceDiagnosticSources.Inspection`
- `ResourceDiagnosticSources.Export`
- `ResourceDiagnosticSources.Viewer`

Create diagnostics through `ResourceInspectionDiagnostics` unless a test is
specifically exercising formatter handling of incomplete/empty fields.

## Codes

Codes are stable automation identifiers. They should not be renamed casually
once exposed through CLI text or JSON.

Naming convention:

```text
wcx.<source-or-domain>.<area>.<condition>
```

Current examples:

- `wcx.payload.canvas.pixelsPartial`
- `wcx.payload.rawData.unsupported`
- `wcx.payload.video.unsupported`
- `wcx.payload.audio.unsupported`
- `wcx.package.link.unresolved`
- `wcx.package.group.shardMissing`
- `wcx.package.group.shardInvalid`
- `wcx.package.pkg2.directoryUnsupported`
- `wcx.package.ms.directoryUnsupported`
- `wcx.export.lua.multipleBlocks`
- `wcx.export.unsupported`
- `wcx.export.canvas.compressionUnsupported`
- `wcx.export.canvas.formatUnsupported`
- `wcx.export.canvas.decodeFailed`
- `wcx.export.binary.outRequired`
- `wcx.export.value.required`
- `wcx.export.value.notFound`
- `wcx.export.value.unsupported`
- `wcx.export.value.ambiguous`
- `wcx.viewer.canvas.compressionUnsupported`
- `wcx.viewer.canvas.formatUnsupported`
- `wcx.viewer.canvas.decodeFailed`
- `wcx.viewer.canvas.linkUnresolved`

Add a code when a diagnostic may be asserted by tests, scripts, future UI, or
automation. Temporary debug facts belong in debug metadata instead.

## Severities

- `info`: expected limitation, partial capability, or non-fatal status.
- `warning`: recoverable issue that may produce incomplete output.
- `error`: request failed or cannot produce the requested output.

Current parser payload diagnostics are `info` because inspection metadata is
still valid even when payload decode coverage is partial. Unsupported export or
viewer requests are `error` because the requested content cannot be produced.

Diagnostic messages should be stable enough for golden CLI output. Avoid
including raw exception text in diagnostic messages; add a new structured field
or debug surface before exposing volatile implementation details.

## Sources

- `parser`: WZ/IMG inspection, payload metadata, or decode limitations.
- `inspection`: Core inspection composition, resource identity, workspace
  linking, or package-group status.
- `export`: export selection, content production, or export-specific status.
- `viewer`: UI/viewer content production, such as a Canvas preview decode
  request.

Future UI should treat source as a filter/grouping hint, not as a replacement
for severity or code.

## CLI Text

CLI text diagnostics are formatted by `ResourceInspectionDiagnosticFormatter`.
The stable text shape is:

```text
<severity> [<code>]: <message> (<path>)
```

`[<code>]` and `(<path>)` are omitted when empty.

Examples:

```text
info [wcx.payload.canvas.pixelsPartial]: Canvas pixel decoding is lazy and currently supports a narrow direct-zlib format slice. (Canvas.img/icon)
error [wcx.export.unsupported]: Selected image is not a supported Lua IMG: Text.img. (Text.img)
```

`inspect --debug` prints diagnostics in the inspection tree. `export` writes
export diagnostics to stderr so stdout remains reserved for exported content.

## JSON

`inspect --debug --json` and `export --type metadata` expose diagnostics as
structured JSON records with the same model fields. Consumers should prefer
`Code`, `Severity`, and `Source` over matching `Message`.

## Adding Diagnostics

When adding a new diagnostic:

1. Decide whether it is a diagnostic or debug metadata.
2. Add a stable code in `ResourceDiagnosticCodes` if it is user-visible or
   automatable.
3. Add a factory method in `ResourceInspectionDiagnostics`.
4. Use named severity/source constants.
5. Add tests for the factory fields and at least one user-visible output path
   when it reaches CLI output.
6. Update this document when introducing a new code family, source, or severity.
