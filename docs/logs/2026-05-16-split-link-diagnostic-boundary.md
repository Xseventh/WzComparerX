# 2026-05-16 Split Link Diagnostic Boundary

- Locked the conservative split-package lookup boundary for non-Base packages:
  if a workspace-relative sibling package is outside the allowed WC-style lookup
  roots, the directory stub stays empty and does not emit an unresolved warning.
- Kept `wcx.package.link.unresolved` scoped to candidate package paths that Core
  actually tries to load and cannot inspect.
- Updated the M5 parser coverage matrix to distinguish unsupported lookup scope
  from attempted-but-failed split-package links.
- Synced `docs/handoff.md` with the recent M5 external-client and PKG2 commits.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
