# 2026-04-28 - Canvas Synthetic Fixture

## Summary

- Added a committed synthetic PKG1 Canvas sample as reviewable hex.
- Added golden CLI coverage for `inspect --debug` against the Canvas fixture.
- Switched the successful `export --type canvas --out` CLI test to use the
  fixed fixture bytes instead of only a test-built temporary package.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
