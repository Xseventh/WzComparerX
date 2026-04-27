# 2026-04-28 Diagnostic Codes

## Summary

- Extended `ResourceInspectionDiagnostic` with optional stable `Code` and
  `Source` fields.
- Added shared diagnostic code constants for parser payload limitations and
  export conditions.
- Updated inspect/export diagnostic formatting so CLI text output includes
  stable codes when present.
- Tagged unsupported Canvas/RawData/Video/Sound payload diagnostics with parser
  codes.
- Tagged Lua multi-block export and unsupported export diagnostics with export
  codes.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Both commands passed.
