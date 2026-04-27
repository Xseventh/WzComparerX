# ADR 0001: Use .NET 10 And Avalonia

## Status

Accepted.

## Context

WC is Windows-first and built around WinForms, DotNetBar, MonoGame WindowsDX,
SharpDX, and native Windows dependencies. WCX should be able to develop and run
core workflows on macOS while preserving a path to a modern desktop UI.

The local development machine has .NET 10 SDK installed. Avalonia templates are
available and support desktop UI across Windows, macOS, and Linux.

## Decision

Use .NET 10 as the initial target framework and Avalonia as the initial desktop
UI framework.

## Consequences

Positive:

- Modern C# and .NET APIs are available.
- Core development works on macOS.
- UI can remain cross-platform.
- Tests and CLI can share the same runtime.

Negative:

- WC code targeting .NET Framework and Windows-only APIs cannot be copied
  blindly.
- Some WC dependencies need replacement or isolation.
- Avalonia is not a direct WinForms port, so UI migration requires redesign.

## Notes

This decision does not require all rendering to use Avalonia. Avatar, map, or
animation rendering may later use dedicated backends behind `WzComparerX.Rendering`.
