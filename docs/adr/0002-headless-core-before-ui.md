# ADR 0002: Build Headless Core Before Feature-Heavy UI

## Status

Accepted.

## Context

WC's main complexity comes from long-lived coupling among UI, global state,
resource lookup, rendering, and file-format logic. A direct UI-first rewrite
would risk recreating that coupling in a newer framework.

## Decision

Build WCX's resource parsing, workspace model, export, search, and comparison
workflows as headless Core and CLI features before investing heavily in desktop
UI.

## Consequences

Positive:

- Behavior can be tested without screenshots or manual UI steps.
- CLI workflows become useful early.
- Avalonia view models can bind to existing services instead of owning parsing.
- Migrated WC behavior can be validated incrementally.

Negative:

- The first visible UI progress may be slower.
- Some UX questions will stay open until the core model stabilizes.

## Notes

The Avalonia project exists from the start so the solution shape is realistic,
but early milestones should not depend on UI implementation.
