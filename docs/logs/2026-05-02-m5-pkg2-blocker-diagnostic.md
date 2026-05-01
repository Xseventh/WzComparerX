# 2026-05-02 M5 PKG2 Blocker Diagnostic

## Summary

- Converted PKG2 directory inspection from an implicit unsupported parser path
  into a stable Core diagnostic.
- Added `wcx.package.pkg2.directoryUnsupported` as the current M5 blocker code
  for PKG2 directory enumeration.
- Kept WzLib PKG2 header detection intact; the new behavior lives in Core
  inspection composition and CLI reporting.

## Behavior

- `inspect --debug --key none <pkg2.wz>` now emits a compact inspection
  document with header debug metadata, the package root, and an error
  diagnostic.
- CLI `inspect` now returns a non-zero exit code when the inspection document
  contains error diagnostics.
- `inspect <pkg2.wz> <selector>` returns the same stable diagnostic through a
  `ResourceInspectionException` because IMG extraction cannot proceed without
  PKG2 directory support.

## Tests

- Added a synthetic PKG2 header fixture in Core tests for deterministic hash
  field and blocker diagnostic coverage.
- Added a CLI golden-style test for `inspect --debug` output and non-zero exit
  behavior.

## Verification

- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 162 total, 0 failed.
