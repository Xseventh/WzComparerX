# ADR 0003: Use WC As Reference, Not Compatibility Target

## Status

Accepted.

## Context

WC contains valuable resource-format knowledge, but also carries historical
coupling to WinForms, DotNetBar, SharpDX, plugin globals, and broad main-form
orchestration.

## Decision

Use WC as a reference implementation and migration source, but do not preserve
its internal APIs, plugin ABI, or UI structure as compatibility targets.

## Consequences

Positive:

- WCX can design cleaner boundaries.
- Old UI and plugin coupling do not constrain the core.
- Migrated behavior can be renamed and reshaped after tests exist.

Negative:

- Existing WC plugins will not load in WCX without adaptation.
- Some migration work requires semantic understanding instead of direct copying.

## Notes

Compatibility should mean resource behavior compatibility, not source-level or
binary compatibility with WC internals.
