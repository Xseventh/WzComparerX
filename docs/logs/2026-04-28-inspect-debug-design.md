# 2026-04-28 - Inspect Debug Design

## Summary

Accepted the design direction that `inspect` should become the single long-term
resource observation surface, with `inspect --debug` replacing `preview-*` as
the development diagnostic view. After migration, preview-specific commands,
services, formatters, and design language should be removed.

## Decisions

- Added ADR 0004 for `inspect --debug`.
- Added a worker brief for implementation.
- Decided that `preview-dir` and `preview-img` are temporary migration scaffolds,
  not compatibility commands to keep.
- Documented that new parser behavior must be reachable through `inspect`.

## Result

Implemented in the follow-up inspect debug iteration. The worker brief was
removed after the implementation landed so future agents start from the current
handoff and command docs rather than a completed task brief.
