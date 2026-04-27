# 2026-04-28 Inspect CLI Golden Tests

## Summary

- Extracted the CLI entrypoint into `CliApplication` so tests can exercise the
  same command parsing and formatter path without spawning a process.
- Added CLI-level golden coverage for `inspect` text output and
  `inspect --debug --json` output against the synthetic fixture.
- Added CLI-level assertions for WZ directory/image `inspect --debug`
  diagnostics, normal inspect output without debug metadata, and rejected
  preview-era command names.
- Cleaned active migration/architecture docs so preview is not presented as a
  current CLI or parser design surface.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed.
