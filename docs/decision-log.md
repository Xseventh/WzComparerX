# Decision Log

This file indexes decisions that shape the project. Larger decisions should have
an ADR under `docs/adr/`.

## Decisions

- 0001: Use .NET 10 and Avalonia for the initial modernization skeleton.
- 0002: Build a headless core and CLI before making the desktop UI feature-heavy.
- 0003: Treat WC as reference implementation material, not as a direct source
  compatibility target.
- 0004: Use `inspect --debug` as the long-term diagnostic and automation
  surface.

## Pending Decisions

- Fixture strategy for real WZ/MS/PKG samples.
- Rendering backend for image, avatar, and map modules.
- CLI command framework, if plain argument parsing becomes insufficient.
- Plugin SDK shape after internal extension points stabilize.
