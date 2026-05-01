# 2026-05-01 Replan Post-M4 Roadmap

## Summary

- Replanned the post-M4 milestone sequence.
- Moved Compare Foundation from Milestone 5 to Milestone 6.
- Defined Milestone 5 as Parser Coverage And Resource Model Baseline.
- Added a WC/WCX capability matrix to `docs/roadmap.md` so future work can be
  selected by feature gap, parser dependency, and validation path.
- Updated `docs/README.md`, `docs/handoff.md`, and `AGENTS.md` so new Codex
  sessions do not continue treating Compare as the immediate Milestone 5.

## Direction

M4 proved that the Avalonia browser can consume Core inspection/export services.
The next risk is not UI viability; it is building Compare, Search, export, and
domain features on parser and resource-identity contracts that are still too
narrow. Milestone 5 should therefore stabilize package parsing, IMG/resource
value coverage, link semantics, split-package source identity, diagnostics, and
real-client smoke coverage before higher-level feature work depends on them.

## Milestone Sequence

- M5: Parser Coverage And Resource Model Baseline.
- M6: Compare Foundation.
- M7: Search And StringLinker Foundation.
- M8: Image And Media Decode / Export Expansion.
- M9: App Browser Maturity.
- M10: Domain Projection Foundation.
- M11+: QuickView/Tooltip, CharaSim, Avatar, MapRender, Patcher, Plugin SDK.

## Notes

The feature backlog continues to treat WC as the reference for format knowledge
and user workflows, not as a UI architecture to port directly. MapRender,
Avatar, and CharaSim remain important successor features, but each should get a
dedicated design plan before implementation.

No code changes were made.
