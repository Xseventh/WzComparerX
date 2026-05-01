# 2026-05-02 M5 Package Group Diagnostics

## Summary

- Added stable package-group diagnostics for `.ini` / `LastWzIndex` shard
  expectations.
- `inspect --debug` now reports warnings on the package root when an
  `.ini`-declared numbered shard is missing or cannot be loaded.
- Kept fallback contiguous shard enumeration silent at the first missing shard,
  since that shape has no explicit manifest declaring a required file.

## Diagnostics

- `wcx.package.group.shardMissing`
  - severity: `warning`
  - source: `inspection`
  - emitted when `Name.ini` declares `Name_###.wz` but the file is absent.
- `wcx.package.group.shardInvalid`
  - severity: `warning`
  - source: `inspection`
  - emitted when a declared numbered shard exists but cannot be read as a valid
    directory package.

## Verification

- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 170 total, 0 failed.
