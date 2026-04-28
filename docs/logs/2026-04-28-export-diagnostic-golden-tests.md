# 2026-04-28 - Export Diagnostic Golden Tests

## Summary

- Added expected stderr fixtures for stable export diagnostics.
- Switched Lua multi-block, Canvas binary-output-required, Canvas unsupported
  compression, Canvas unsupported format, and unsupported export tests from
  substring checks to exact golden output assertions.
- Documented that `fixtures/expected` can hold stdout/stderr regression outputs.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
