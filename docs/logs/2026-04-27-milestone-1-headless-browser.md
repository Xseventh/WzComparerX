# 2026-04-27 Milestone 1 Headless Browser

## Summary

Implemented the first headless resource browser path using a small synthetic
fixture.

## Completed

- Added a minimal raw resource document/node model in `WzComparerX.WzLib`.
- Added a synthetic JSON document reader for safe committed fixtures.
- Added Core document, workspace, and deterministic tree listing services.
- Added `fixtures/synthetic/basic-tree.json`.
- Implemented the CLI `list` command.
- Replaced bootstrap placeholder tests with fixture-backed WzLib/Core tests.
- Verified build and tests.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- list fixtures/synthetic/basic-tree.json
```

The regular `dotnet run --project src/WzComparerX.Cli -- list ...` command
hung in the Codex macOS sandbox during the SDK build/run step after printing a
CSSM warning. The same entry point succeeds with `--no-build` after a successful
solution build, and direct DLL execution also succeeds.

## Next Suggested Work

Begin Milestone 2 by choosing one smallest real WC parser behavior to migrate,
preferably a header/version or directory-tree enumeration behavior with a tiny
fixture-backed test.
