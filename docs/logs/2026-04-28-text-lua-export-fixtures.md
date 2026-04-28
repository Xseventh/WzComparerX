# 2026-04-28 - Text And Lua Export Fixtures

## Summary

- Added committed PKG1 hex fixtures for a WC text-format IMG and a Lua IMG.
- Added expected stdout fixtures for text and Lua export.
- Switched the successful `export --type text` and `export --type lua` CLI
  tests from test-built temporary packages to fixed fixture bytes.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
