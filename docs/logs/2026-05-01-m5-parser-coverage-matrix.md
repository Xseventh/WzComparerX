# 2026-05-01 M5 Parser Coverage Matrix

## Summary

- Added `docs/parser-coverage-matrix.md` as the working checklist for
  Milestone 5 Parser Coverage And Resource Model Baseline.
- Split the high-level roadmap capability matrix into actionable M5 tables for
  package formats, IMG values, resource identity, diagnostics, and real-client
  smoke coverage.
- Updated `docs/roadmap.md`, `docs/README.md`, and `docs/handoff.md` so future
  work uses the new M5 matrix before starting Compare/Search/export expansion.

## Direction

The latest roadmap moves Compare Foundation to M6. The immediate M5 focus is
parser/resource-model confidence: package groups, PKG2 foundation, link
semantics, explicit source identity, stable diagnostics, and real-client smoke
coverage.

## Next Slice Candidates

- Make Core inspection identity fields explicit enough for package source,
  image selector, value path, and linked targets.
- Add diagnostics for unresolved split-package and link targets.
- Choose the first PKG2 directory parsing slice or document the blocker.
- Record representative local-client smoke notes across package families.
