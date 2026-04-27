# 2026-04-28 Export Abstraction

## Summary

- Added the first Core export model and `ResourceExportService`.
- Added CLI `export` with `--type metadata`, `--type text`, and `--type lua`.
- Metadata export reuses the stable inspection JSON shape from
  `inspect --debug --json`.
- Text export writes original WC text-format IMG streams.
- Lua export writes the full decoded script for supported Lua IMG blocks while
  normal inspection continues to show only a short snippet.
- Added CLI-level tests for metadata/text/Lua export and WzLib tests for
  retained text/Lua source payloads.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed.
