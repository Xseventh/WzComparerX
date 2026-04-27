# ADR 0004: Use Inspect Debug For Diagnostics

## Status

Accepted and implemented.

## Context

WCX had grown migration-oriented diagnostic commands alongside the first stable
Core `inspect` model. Keeping both as equal product surfaces would create a
long-term risk: command behavior, key detection, image selection, property
depth, JSON output, and unsupported-case reporting could diverge.

The migration diagnostics were useful while migrating WC parser behavior because
they exposed low-level fields such as node type bytes, offsets,
checksums, hash offsets, and payload metadata. Those fields are important for
development, but they should not become a second user-facing resource model.

## Decision

Use `inspect` as the long-term resource observation surface for CLI, UI, export,
search, and automation workflows.

Add an `inspect --debug` mode for stable development diagnostics. Debug mode may
expose lower-level parser metadata, but it must still be projected through the
Core inspection model rather than implementing a separate parser path.

Once `inspect --debug` covers the useful diagnostic fields, remove the
migration diagnostic commands, command-specific design language, and command
documentation. New real parser behavior should be considered complete only
after it is reachable through `inspect`.

## Consequences

Positive:

- CLI, future UI, export, and automation share one resource model.
- Parser migration can keep useful low-level diagnostics without growing a
  second product API.
- Key detection, selector resolution, property-depth behavior, and diagnostics
  have one preferred implementation path.

Negative:

- The inspection model must carry structured metadata and diagnostics sooner.
- Some existing diagnostic formatter behavior needs to be re-projected into
  `inspect --debug` before command-specific code and documentation can be
  deleted.

## Rules

- WzLib remains the single source of resource parsing behavior.
- Core inspection services project parser results into stable nodes, metadata,
  and diagnostics.
- `inspect` is the preferred CLI surface.
- `inspect --debug` is the preferred diagnostic CLI surface.
- Separate migration diagnostic commands must not gain new business logic that is
  absent from `inspect`.
- After migration, delete migration diagnostic commands, command-specific
  formatters and services, and design documentation that presents them as a
  supported surface.
