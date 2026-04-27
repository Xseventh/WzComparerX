# 2026-04-27 JSON CLI Output

## Summary

Added machine-readable JSON output for existing CLI browse and header
workflows.

## Completed

- Added `ResourceTreeJsonFormatter`.
- Added `WzPackageHeaderJsonFormatter`.
- Added optional `--json` support:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- list --json fixtures/synthetic/basic-tree.json
dotnet run --project src/WzComparerX.Cli --no-build -- header --json path/to/file.wz
```

- Kept existing text output unchanged.
- Added Core tests for JSON resource tree and header output.
- Marked computed raw-node helper properties as ignored for JSON output.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- list --json fixtures/synthetic/basic-tree.json
```

## Next Suggested Work

Add a general `inspect` command facade so `list`, `header`, and future node
inspection can share output format handling instead of adding command-specific
format branches forever.
