# 2026-04-28 Export Output And Reader Split

## Summary

- Changed export documents to carry byte payloads, content type, and optional
  structured diagnostics.
- Added CLI `export --out <path>` for exact file output while keeping text
  exports on stdout by default.
- Defined the current stdout/file-output convention: stdout is for text payloads
  only, diagnostics go to stderr, and future binary exporters should use
  `--out`.
- Fixed Lua multi-block export to concatenate decoded blocks in stream order
  without inserting separators.
- Added structured export diagnostics for unsupported export requests.
- Split Lua and text IMG inspection readers out of `WzImageInspectionReader`.
- Added tests for Lua multi-block export, `--out`, and unsupported export
  diagnostics.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed.
