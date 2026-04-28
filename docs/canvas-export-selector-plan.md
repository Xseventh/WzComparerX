# Canvas Export Selector Plan

Canvas export originally selected the first Canvas value found in the selected
IMG. The current CLI uses an explicit `--value <path>` selector for Canvas
values inside an IMG.

## Goal

Let callers select a specific Canvas value inside an IMG through the same
inspection paths that `inspect --debug` prints.

## CLI Shape

Keep the existing positional selector as the IMG selector:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- export --type canvas --out icon.raw --value icon --key auto path/to/UI.wz Basic.img
dotnet run --project src/WzComparerX.Cli --no-build -- export --type canvas --out frame.raw --value button/normal/0 --key auto path/to/UI.wz Basic.img
```

Rules:

- `--value <path>` is optional for non-Canvas exports at first.
- `--value <path>` is required for Canvas export, except when the selected IMG
  root object itself is a Canvas.
- The value path is matched against `WzImagePropertyInspectionEntry.Path`.
- Matching is exact and case-sensitive at first. Do not add globbing or fuzzy
  search until real workflows need it.
- If no value matches, return a structured export diagnostic.
- If the value exists but is not Canvas, return a structured export diagnostic.
- If multiple values somehow share a path, fail with a diagnostic instead of
  choosing one.

## Core Shape

The nullable value selector lives on `ResourceExportOptions`, not as another
positional CLI argument. That keeps the package/image selector and the
inside-IMG value selector separate.

Canvas export should then:

1. Load the selected IMG inspection.
2. If the IMG root object is Canvas and no `--value` is supplied, use it.
3. Otherwise require `--value`.
4. Resolve the value selector against flattened property paths.
5. Decode/export only that Canvas payload.

## Diagnostics

Stable export diagnostics:

- `wcx.export.value.required`
- `wcx.export.value.notFound`
- `wcx.export.value.unsupported`
- `wcx.export.value.ambiguous`

Diagnostic paths should include the IMG selector and value selector when both
are known.

## M3 Boundary

Raw Canvas bytes are enough for M3 as a parser/export foundation. PNG export is
not part of M3. Canvas raw export now has explicit value selection,
deterministic CLI golden tests, and diagnostics for missing or wrong value
selectors.
