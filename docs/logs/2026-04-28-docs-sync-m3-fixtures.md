# 2026-04-28 - M3 Fixture Docs Sync

## Summary

- Updated command docs to reflect the current direct-zlib Canvas raw export
  slice instead of describing all Canvas export as future work.
- Added local CLI smoke commands for the committed text, Lua, and Canvas hex
  fixtures.
- Updated roadmap and handoff to mention committed fixture/golden coverage for
  text, Lua, and Canvas export paths.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
