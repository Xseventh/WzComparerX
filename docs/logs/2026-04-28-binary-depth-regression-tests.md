# 2026-04-28 - Binary Depth Regression Tests

## Summary

- Added deterministic WzLib regression coverage for binary IMG depth behavior.
- Locked ordinary nested `Property` handling when `--depth` stops at the
  parent: children are not expanded and the following sibling property still
  parses correctly.
- Locked Canvas mini-property handling when `--depth` stops at the parent:
  mini-property metadata is still consumed so payload offset/length metadata
  remains correct.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
