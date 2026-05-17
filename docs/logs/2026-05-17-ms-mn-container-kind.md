# 2026-05-17 MS MN Container Kind

- Preserved the actual `.ms` / `.mn` container kind in Core inspection output.
- `.ms` continues to report `format: ms` and `containerKind: ms`.
- `.mn` now reports `format: mn`, display value `mn`, and
  `containerKind: mn`, while still using the shared WC-style MS/MN reader path.
- Reused the same container-kind helper across folder inspection, direct
  inspection, Canvas preview, and link resolution.

Validation:

- Targeted Core tests passed for folder MS/MN listing, `.ms` CLI inspection,
  `.mn` direct inspection, `.mn` CLI inspection, and MS outlink Canvas preview.
- Full solution build/test passed.
- Optional real GMS `ResourceInspectionExternalClientSmokeTests` passed: 14/14.
