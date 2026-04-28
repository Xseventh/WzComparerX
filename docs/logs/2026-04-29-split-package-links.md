# 2026-04-29 - Split Package Links

## Summary

Added workspace-level split-package linking for GMS-style package indices.

## Changes

- Core `inspect` now detects empty PKG1 directory stubs at any depth when they
  have no parsed children.
- Link resolution checks both paths relative to the current package directory
  and paths relative to the broader client data workspace.
- This covers root index cases such as `Base/Base.wz` -> `Effect/Effect.wz`
  and same-package subtree cases such as `UI/UI.wz` -> `UI/_Canvas/_Canvas.wz`.
- Linked packages are grafted under the stub directory as package nodes, so the
  Avalonia tree can expand `Base.wz` categories without App-specific path
  guessing.
- Linked package roots keep their full package paths so UI package-open actions
  can navigate to them.
- Added deterministic Core coverage with temp top-level and nested
  split-package layouts.
- Smoke-tested local GMS `Base/Base.wz`; `Effect` now expands into linked
  `Effect.wz` and `Effect_000.wz` package nodes.
- Smoke-tested local GMS `UI/UI.wz`; `_Canvas` now expands into `_Canvas.wz`
  and `_Canvas_*.wz` package nodes.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
