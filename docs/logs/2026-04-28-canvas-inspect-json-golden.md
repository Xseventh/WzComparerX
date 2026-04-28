# 2026-04-28 - Canvas Inspect JSON Golden

## Summary

- Added a golden JSON output for `inspect --debug --json` against the committed
  direct-zlib Canvas fixture.
- Added a CLI regression test that compares the full normalized JSON output.
- Reused the expected fixture helper for both text and JSON inspect golden
  outputs.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
