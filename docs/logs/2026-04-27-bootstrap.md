# 2026-04-27 Bootstrap

## Summary

Started WCX as a long-running modernization project on branch
`codex/wcx-modernization`.

## Completed

- Installed/verified local tools:
  - .NET SDK `10.0.203`
  - ripgrep `15.1.0`
  - ffmpeg `8.1_1`
  - ImageMagick `7.1.2-21`
- Added `/usr/local/share/dotnet` to `~/.zshrc`.
- Created `.NET 10` solution skeleton.
- Added Avalonia MVVM app.
- Added CLI, Core, Domain, Rendering, WzLib, and test projects.
- Added architecture, migration, format, and fixture planning docs.
- Verified build and tests.
- Committed initial project skeleton:
  - `a0858cb Bootstrap WCX modernization project`

## Current Branch

```text
codex/wcx-modernization
```

## Important Notes

- `origin` still points to `https://github.com/Kagamia/WzComparerX.git`.
- Do not push to upstream unless the user explicitly asks and the remote strategy
  is clarified.
- Build/test may require elevated permissions in Codex because MSBuild named
  pipes can be blocked by the sandbox.

## Next Suggested Work

Implement Milestone 1:

- Minimal raw node model in `WzComparerX.WzLib`.
- Workspace/document model in `WzComparerX.Core`.
- `wcx list` CLI command.
- Tests for deterministic tree listing.
