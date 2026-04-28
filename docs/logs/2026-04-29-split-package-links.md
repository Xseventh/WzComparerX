# 2026-04-29 - Split Package Links

## Summary

Added the first workspace-level split-package linking for GMS-style package
indices.

## Changes

- Core `inspect` now detects empty top-level PKG1 directory stubs and resolves
  sibling package directories such as `Base/Base.wz` -> `Effect/Effect.wz` and
  `Effect/Effect_000.wz`.
- Linked packages are grafted under the stub directory as package nodes, so the
  Avalonia tree can expand `Base.wz` categories without App-specific path
  guessing.
- Linked package roots keep their full package paths so UI package-open actions
  can navigate to them.
- Added deterministic Core coverage with a temp split-package layout.
- Smoke-tested local GMS `Base/Base.wz`; `Effect` now expands into linked
  `Effect.wz` and `Effect_000.wz` package nodes.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
